using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Api.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace Maliev.OrderService.Api.Authorization
{
    /// <summary>
    /// Authorization handler that validates a user's permissions against the required permission.
    /// Implements Redis caching, structured audit logging, and business metrics.
    /// </summary>
    public partial class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IIamServiceClient _iamClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PermissionAuthorizationHandler> _logger;
        private readonly KeyValuePair<string, object?>[] _defaultTags;

        // Metrics
        private readonly Counter<long> _authSuccessCounter;
        private readonly Counter<long> _authFailureCounter;
        private readonly Histogram<double> _authCheckDuration;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _userSemaphores = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionAuthorizationHandler"/> class.
        /// </summary>
        /// <param name="iamClient">The IAM service client.</param>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="meterFactory">The meter factory for metrics.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger instance.</param>
        public PermissionAuthorizationHandler(
            IIamServiceClient iamClient,
            IDistributedCache cache,
            IMeterFactory meterFactory,
            IConfiguration configuration,
            ILogger<PermissionAuthorizationHandler> logger)
        {
            _iamClient = iamClient;
            _cache = cache;
            _logger = logger;

            string serviceName = configuration["Service:Name"] ?? "OrderService";
            Meter meter = meterFactory.Create($"{serviceName.ToLowerInvariant()}-authorization-meter");

            _defaultTags =
            [
                new KeyValuePair<string, object?>("service_name", serviceName),
                new KeyValuePair<string, object?>("version", configuration["Service:Version"] ?? "1.0.0"),
                new KeyValuePair<string, object?>("region", configuration["Service:Region"] ?? "global"),
                new KeyValuePair<string, object?>("environment", configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production")
            ];

            _authSuccessCounter = meter.CreateCounter<long>("auth.permission.success", "count", "Number of successful permission checks");
            _authFailureCounter = meter.CreateCounter<long>("auth.permission.failure", "count", "Number of failed permission checks");
            _authCheckDuration = meter.CreateHistogram<double>("auth.permission.duration", "ms", "Latency of permission checks");
        }

        /// <summary>
        /// Handles the authorization requirement.
        /// </summary>
        /// <param name="context">The authorization context.</param>
        /// <param name="requirement">The permission requirement.</param>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            long startTime = Stopwatch.GetTimestamp();
            string userId = context.User.GetUserId();

            var tags = new TagList();
            foreach (KeyValuePair<string, object?> tag in _defaultTags)
            {
                tags.Add(tag);
            }
            tags.Add("permission", requirement.Permission);

            try
            {
                IEnumerable<string> permissions = await GetUserPermissionsAsync(userId);

                if (permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                    _authSuccessCounter.Add(1, tags);
                }
                else
                {
                    // T007.1: Structured Audit Logging for 403s
                    Log.AuthorizationFailed(_logger, userId, requirement.Permission);

                    tags.Add("reason", "missing_permission");
                    _authFailureCounter.Add(1, tags);
                }
            }
            catch (Exception ex)
            {
                Log.AuthorizationError(_logger, userId, requirement.Permission, ex);

                tags.Add("reason", "error");
                _authFailureCounter.Add(1, tags);
                // Fail-secure: context.Succeed is NOT called
            }
            finally
            {
                double duration = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
                _authCheckDuration.Record(duration, tags);
            }
        }

        private async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
        {
            string cacheKey = $"user_permissions:{userId}";
            string? cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<string>>(cachedData) ?? Enumerable.Empty<string>();
            }

            // Thundering herd protection
            SemaphoreSlim semaphore = _userSemaphores.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                // Double-check cache after acquiring lock
                cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    return JsonSerializer.Deserialize<List<string>>(cachedData) ?? Enumerable.Empty<string>();
                }

                // Cache miss - Fetch from IAM
                var permissions = (await _iamClient.GetUserPermissionsAsync(userId)).ToList();

                // Cache for 5-10 minutes (using 5 for consistency with recommended minimum)
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(permissions), cacheOptions);

                return permissions;
            }
            finally
            {
                _ = semaphore.Release();
                // DONT cleanup semaphore based on CurrentCount to avoid race condition with TryRemove.
                // Semaphores are small; let them be handled by the ConcurrentDictionary or periodically cleaned.
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "AUDIT: Authorization failed. PrincipalId={UserId}, RequiredPermission={Permission}, Result=Forbidden")]
            public static partial void AuthorizationFailed(ILogger logger, string userId, string permission);

            [LoggerMessage(Level = LogLevel.Error, Message = "Error during authorization check for user {UserId} and permission {Permission}")]
            public static partial void AuthorizationError(ILogger logger, string userId, string permission, Exception ex);
        }
    }
}

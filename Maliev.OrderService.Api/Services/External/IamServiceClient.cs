using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Client for interacting with the central IAM service.
/// </summary>
public partial class IamServiceClient : IIamServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IamServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IamServiceClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public IamServiceClient(IHttpClientFactory httpClientFactory, ILogger<IamServiceClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("IAMService");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/iam/v1/users/{userId}/permissions", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                Log.FailedToFetchPermissions(_logger, userId, response.StatusCode);
                return Enumerable.Empty<string>();
            }

            var result = await response.Content.ReadFromJsonAsync<UserPermissionsResponse>(cancellationToken: cancellationToken);
            return result?.Permissions ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            Log.ErrorFetchingPermissions(_logger, userId, ex);
            return Enumerable.Empty<string>();
        }
    }

    private sealed record UserPermissionsResponse(string UserId, List<string> Permissions);

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch permissions for user {UserId}. Status: {StatusCode}")]
        public static partial void FailedToFetchPermissions(ILogger logger, string userId, System.Net.HttpStatusCode statusCode);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error occurred while fetching permissions for user {UserId}")]
        public static partial void ErrorFetchingPermissions(ILogger logger, string userId, Exception ex);
    }
}

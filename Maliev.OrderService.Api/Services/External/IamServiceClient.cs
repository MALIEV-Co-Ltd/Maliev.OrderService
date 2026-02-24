namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the central IAM service.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="IamServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public partial class IamServiceClient(IHttpClientFactory httpClientFactory, ILogger<IamServiceClient> logger) : IIamServiceClient
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient("IAMService");
        private readonly ILogger<IamServiceClient> _logger = logger;

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/iam/v1/users/{userId}/permissions", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Log.FailedToFetchPermissions(_logger, userId, response.StatusCode);
                    return [];
                }

                UserPermissionsResponse? result = await response.Content.ReadFromJsonAsync<UserPermissionsResponse>(cancellationToken: cancellationToken);
                return result?.Permissions ?? Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                Log.ErrorFetchingPermissions(_logger, userId, ex);
                return [];
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
}

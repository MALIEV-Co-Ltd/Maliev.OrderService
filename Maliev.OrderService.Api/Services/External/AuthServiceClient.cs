namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the external Auth Service
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AuthServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public partial class AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger) : IAuthServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<AuthServiceClient> _logger = logger;

        /// <inheritdoc />
        public async Task<UserContextDto?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/validate");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<UserContextDto>(cancellationToken: cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToValidateToken(_logger, ex);
                return null;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to validate authentication token")]
            public static partial void FailedToValidateToken(ILogger logger, Exception ex);
        }
    }
}

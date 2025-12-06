using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Client for interacting with the external Auth Service
/// </summary>
public partial class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthServiceClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserContextDto?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/validate");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

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

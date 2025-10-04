using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

public partial class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthServiceClient> _logger;

    public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

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

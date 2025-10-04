using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

public partial class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentStatusDto?> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/payments/{paymentId}/status", cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaymentStatusDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToGetPaymentStatus(_logger, paymentId, ex);
            return null;
        }
    }

    public async Task<decimal> CalculatePartialChargeAsync(string orderId, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { orderId, status };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/payments/calculate-partial-charge", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PartialChargeResponse>(cancellationToken: cancellationToken);
            return result?.Amount ?? 0;
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToCalculatePartialCharge(_logger, orderId, ex);
            return 0;
        }
    }

    private sealed class PartialChargeResponse
    {
        public decimal Amount { get; set; }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get payment status for {PaymentId}")]
        public static partial void FailedToGetPaymentStatus(ILogger logger, string paymentId, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to calculate partial charge for order {OrderId}")]
        public static partial void FailedToCalculatePartialCharge(ILogger logger, string orderId, Exception ex);
    }
}

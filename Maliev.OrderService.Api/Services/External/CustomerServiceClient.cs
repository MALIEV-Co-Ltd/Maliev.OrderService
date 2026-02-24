namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the external Customer Service
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CustomerServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public partial class CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger) : ICustomerServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<CustomerServiceClient> _logger = logger;

        /// <inheritdoc />
        public async Task<bool> HasActiveNdaAsync(string customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/customer/v1/ndas/customer/{customerId}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                NdaStatusResponse? result = await response.Content.ReadFromJsonAsync<NdaStatusResponse>(cancellationToken: cancellationToken);
                return result?.HasActiveNda ?? false;
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToCheckNdaStatus(_logger, customerId, ex);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<CustomerDetailsDto?> GetCustomerDetailsAsync(string customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/customer/v1/customers/{customerId}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToGetCustomerDetails(_logger, customerId, ex);
                return null;
            }
        }

        private sealed class NdaStatusResponse
        {
            public bool HasActiveNda { get; set; }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to check NDA status for customer {CustomerId}")]
            public static partial void FailedToCheckNdaStatus(ILogger logger, string customerId, Exception ex);

            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get customer details for {CustomerId}")]
            public static partial void FailedToGetCustomerDetails(ILogger logger, string customerId, Exception ex);
        }
    }
}

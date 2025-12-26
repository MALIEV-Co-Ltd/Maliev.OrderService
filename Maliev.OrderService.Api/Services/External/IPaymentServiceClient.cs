namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Interface for interacting with the external Payment Service
    /// </summary>
    public interface IPaymentServiceClient
    {
        /// <summary>
        /// Gets the payment status for a given payment ID
        /// </summary>
        /// <param name="paymentId">The payment ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The payment status or null if not found</returns>
        Task<PaymentStatusDto?> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the partial charge amount for an order based on its status
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="status">The order status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The calculated partial charge amount</returns>
        Task<decimal> CalculatePartialChargeAsync(string orderId, string status, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Data transfer object for payment status
    /// </summary>
    public class PaymentStatusDto
    {
        /// <summary>Gets or sets the payment ID</summary>
        public required string PaymentId { get; set; }

        /// <summary>Gets or sets the payment status (Unpaid, Paid, POIssued, PartiallyRefunded, Refunded)</summary>
        public required string Status { get; set; }

        /// <summary>Gets or sets the payment amount</summary>
        public decimal Amount { get; set; }

        /// <summary>Gets or sets the currency code</summary>
        public required string Currency { get; set; }
    }
}

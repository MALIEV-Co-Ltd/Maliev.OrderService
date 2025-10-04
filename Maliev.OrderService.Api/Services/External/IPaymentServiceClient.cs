namespace Maliev.OrderService.Api.Services.External;

public interface IPaymentServiceClient
{
    Task<PaymentStatusDto?> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default);
    Task<decimal> CalculatePartialChargeAsync(string orderId, string status, CancellationToken cancellationToken = default);
}

public class PaymentStatusDto
{
    public required string PaymentId { get; set; }
    public required string Status { get; set; } // Unpaid, Paid, POIssued, PartiallyRefunded, Refunded
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
}

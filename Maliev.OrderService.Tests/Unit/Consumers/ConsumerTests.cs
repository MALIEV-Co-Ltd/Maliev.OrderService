using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Payments;
using Maliev.OrderService.Api.Consumers;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Services.Business;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.OrderService.Tests.Unit.Consumers
{
    public class ConsumerTests
    {
        private readonly Mock<IOrderStatusService> _statusServiceMock = new();
        private readonly Mock<ILogger<PaymentCompletedEventConsumer>> _paymentLoggerMock = new();

        [Fact]
        public async Task PaymentCompletedEventConsumerSuccessUpdatesStatus()
        {
            // Arrange
            _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            var orderId = Guid.NewGuid();
            var message = new PaymentCompletedEvent
            {
                Payload = new PaymentCompletedEventPayload
                {
                    OrderId = orderId,
                    OrderNumber = orderId.ToString(),
                    PaymentId = Guid.NewGuid(),
                    Amount = 100,
                    Currency = "USD"
                }
            };
            contextMock.Setup(c => c.Message).Returns(message);

            _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                default))
                .ReturnsAsync(new OrderStatusResponse
                {
                    StatusId = 1,
                    OrderId = orderId.ToString(),
                    Status = "Paid",
                    UpdatedBy = "System",
                    Timestamp = DateTime.UtcNow
                });

            // Act
            await consumer.Consume(contextMock.Object);

            // Assert
#pragma warning disable CA1873
            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                orderId.ToString(),
                It.Is<CreateOrderStatusRequest>(r => r.Status == "Paid"),
                "System-PaymentService",
                default), Times.Once);
#pragma warning restore CA1873
        }

        [Fact]
        public async Task PaymentCompletedEventConsumerErrorThrows()
        {
            // Arrange
            _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            var message = new PaymentCompletedEvent
            {
                Payload = new PaymentCompletedEventPayload { OrderId = Guid.NewGuid(), OrderNumber = "1" }
            };
            contextMock.Setup(c => c.Message).Returns(message);

            _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(It.IsAny<string>(), It.IsAny<CreateOrderStatusRequest>(), It.IsAny<string>(), default))
                .ThrowsAsync(new InvalidOperationException("Order not found"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(contextMock.Object));
        }

        [Fact]
        public async Task PaymentCompletedEventConsumerDuplicatePaidStatusForSamePaymentDoesNotThrow()
        {
            // Arrange
            _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var message = new PaymentCompletedEvent
            {
                Payload = new PaymentCompletedEventPayload
                {
                    OrderId = orderId,
                    OrderNumber = orderId.ToString(),
                    PaymentId = paymentId,
                    Amount = 100,
                    Currency = "USD"
                }
            };
            contextMock.Setup(c => c.Message).Returns(message);
            contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

            _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Invalid transition from Paid to Paid"));

            _statusServiceMock.Setup(s => s.GetOrderStatusHistoryAsync(orderId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderStatusResponse>
                {
                    new()
                    {
                        StatusId = 12,
                        OrderId = orderId.ToString(),
                        Status = "Paid",
                        InternalNotes = $"Payment {paymentId} completed - Amount: 100 USD",
                        UpdatedBy = "System-PaymentService",
                        Timestamp = DateTime.UtcNow
                    }
                });

            // Act
            await consumer.Consume(contextMock.Object);

            // Assert
            _statusServiceMock.Verify(s => s.GetOrderStatusHistoryAsync(
                orderId.ToString(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

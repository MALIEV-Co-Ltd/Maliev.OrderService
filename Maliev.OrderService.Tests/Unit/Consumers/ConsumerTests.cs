using Maliev.MessagingContracts.Contracts.Jobs;
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
        private readonly Mock<ILogger<PaymentCancelledEventConsumer>> _paymentCancelledLoggerMock = new();
        private readonly Mock<ILogger<PaymentExpiredEventConsumer>> _paymentExpiredLoggerMock = new();
        private readonly Mock<ILogger<PaymentFailedEventConsumer>> _paymentFailedLoggerMock = new();
        private readonly Mock<ILogger<JobStatusChangedEventConsumer>> _jobStatusLoggerMock = new();

        [Fact]
        public async Task PaymentCompletedEventConsumerSuccessUpdatesStatus()
        {
            // Arrange
            _ = _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            var orderId = Guid.NewGuid();
            var message = new PaymentCompletedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new PaymentCompletedEventPayload
                {
                    OrderId = orderId,
                    OrderNumber = orderId.ToString(),
                    PaymentId = Guid.NewGuid(),
                    Amount = 100,
                    Currency = "USD"
                }
            };
            _ = contextMock.Setup(c => c.Message).Returns(message);

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
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
            _ = _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            var message = new PaymentCompletedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new PaymentCompletedEventPayload { OrderId = Guid.NewGuid(), OrderNumber = "1" }
            };
            _ = contextMock.Setup(c => c.Message).Returns(message);

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(It.IsAny<string>(), It.IsAny<CreateOrderStatusRequest>(), It.IsAny<string>(), default))
                .ThrowsAsync(new InvalidOperationException("Order not found"));

            // Act & Assert
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(contextMock.Object));
        }

        [Fact]
        public async Task PaymentCompletedEventConsumerDuplicatePaidStatusForSamePaymentDoesNotThrow()
        {
            // Arrange
            _ = _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            _ = _paymentLoggerMock.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var message = new PaymentCompletedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new PaymentCompletedEventPayload
                {
                    OrderId = orderId,
                    OrderNumber = orderId.ToString(),
                    PaymentId = paymentId,
                    Amount = 100,
                    Currency = "USD"
                }
            };
            _ = contextMock.Setup(c => c.Message).Returns(message);
            _ = contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Invalid transition from Paid to Paid"));

            _ = _statusServiceMock.Setup(s => s.GetOrderStatusHistoryAsync(orderId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new()
                    {
                        StatusId = 12,
                        OrderId = orderId.ToString(),
                        Status = "Paid",
                        InternalNotes = $"Payment {paymentId} completed - Amount: 100 USD",
                        UpdatedBy = "System-PaymentService",
                        Timestamp = DateTime.UtcNow
                    }
                ]);

            // Act
            await consumer.Consume(contextMock.Object);

            // Assert
            _statusServiceMock.Verify(s => s.GetOrderStatusHistoryAsync(
                orderId.ToString(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PaymentCompletedEventConsumerWithoutOrderServiceRoutingIsIgnored()
        {
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentCompletedEvent
            {
                ConsumedBy = ["InvoiceService", "NotificationService"],
                Payload = new PaymentCompletedEventPayload
                {
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-123",
                    PaymentId = Guid.NewGuid(),
                    Amount = 100,
                    Currency = "THB"
                }
            });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PaymentCompletedEventConsumerWithoutPayloadIsIgnored()
        {
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentCompletedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = null!
            });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PaymentCompletedEventConsumerWithoutRoutingListIsIgnored()
        {
            var consumer = new PaymentCompletedEventConsumer(_statusServiceMock.Object, _paymentLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCompletedEvent>>();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentCompletedEvent
            {
                ConsumedBy = null!,
                Payload = new PaymentCompletedEventPayload
                {
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-2026-NULL-ROUTE",
                    PaymentId = Guid.NewGuid(),
                    Amount = 100,
                    Currency = "THB"
                }
            });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PaymentCancelledEventConsumerWithOrderServiceRoutingCancelsOrder()
        {
            var consumer = new PaymentCancelledEventConsumer(_statusServiceMock.Object, _paymentCancelledLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCancelledEvent>>();
            var transactionId = Guid.NewGuid();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentCancelledEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new PaymentCancelledEventPayload
                {
                    TransactionId = transactionId,
                    IdempotencyKey = "payment-cancelled",
                    Amount = 100,
                    Currency = "THB",
                    CustomerId = "customer-1",
                    OrderId = "ORD-CANCELLED-1",
                    ProviderName = "stripe",
                    Reason = "Customer cancelled checkout",
                    ProviderEventCode = "checkout.session.expired",
                    CancelledAt = DateTimeOffset.UtcNow
                }
            });

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<CreateOrderStatusRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderStatusResponse
                {
                    StatusId = 1,
                    OrderId = "ORD-CANCELLED-1",
                    Status = "Cancelled",
                    UpdatedBy = "System-PaymentService",
                    Timestamp = DateTime.UtcNow
                });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                "ORD-CANCELLED-1",
                It.Is<CreateOrderStatusRequest>(r =>
                    r.Status == "Cancelled" &&
                    r.InternalNotes != null &&
                    r.InternalNotes.Contains(transactionId.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    r.InternalNotes.Contains("Customer cancelled checkout", StringComparison.OrdinalIgnoreCase)),
                "System-PaymentService",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PaymentCancelledEventConsumerWithoutOrderServiceRoutingIsIgnored()
        {
            var consumer = new PaymentCancelledEventConsumer(_statusServiceMock.Object, _paymentCancelledLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCancelledEvent>>();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentCancelledEvent
            {
                ConsumedBy = ["NotificationService"],
                Payload = new PaymentCancelledEventPayload
                {
                    TransactionId = Guid.NewGuid(),
                    OrderId = "ORD-IGNORED",
                    Reason = "ignored"
                }
            });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PaymentCancelledEventConsumerDuplicateCancellationForSameTransactionDoesNotThrow()
        {
            var consumer = new PaymentCancelledEventConsumer(_statusServiceMock.Object, _paymentCancelledLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentCancelledEvent>>();
            var transactionId = Guid.NewGuid();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentCancelledEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new PaymentCancelledEventPayload
                {
                    TransactionId = transactionId,
                    OrderId = "ORD-CANCELLED-DUP",
                    Reason = "Customer cancelled checkout"
                }
            });
            _ = contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<CreateOrderStatusRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Invalid transition from Cancelled to Cancelled"));
            _ = _statusServiceMock.Setup(s => s.GetOrderStatusHistoryAsync(
                    "ORD-CANCELLED-DUP",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new()
                    {
                        StatusId = 2,
                        OrderId = "ORD-CANCELLED-DUP",
                        Status = "Cancelled",
                        InternalNotes = $"Payment {transactionId} cancelled - Customer cancelled checkout",
                        UpdatedBy = "System-PaymentService",
                        Timestamp = DateTime.UtcNow
                    }
                ]);

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.GetOrderStatusHistoryAsync(
                "ORD-CANCELLED-DUP",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PaymentExpiredEventConsumerWithOrderServiceRoutingCancelsOrder()
        {
            var consumer = new PaymentExpiredEventConsumer(_statusServiceMock.Object, _paymentExpiredLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentExpiredEvent>>();
            var transactionId = Guid.NewGuid();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentExpiredEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new PaymentExpiredEventPayload
                {
                    TransactionId = transactionId,
                    IdempotencyKey = "payment-expired",
                    Amount = 100,
                    Currency = "THB",
                    CustomerId = "customer-1",
                    OrderId = "ORD-EXPIRED-1",
                    ProviderName = "stripe",
                    Reason = "Checkout session expired",
                    ProviderEventCode = "checkout.session.expired",
                    ExpiredAt = DateTimeOffset.UtcNow
                }
            });

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<CreateOrderStatusRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderStatusResponse
                {
                    StatusId = 1,
                    OrderId = "ORD-EXPIRED-1",
                    Status = "Cancelled",
                    UpdatedBy = "System-PaymentService",
                    Timestamp = DateTime.UtcNow
                });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                "ORD-EXPIRED-1",
                It.Is<CreateOrderStatusRequest>(r =>
                    r.Status == "Cancelled" &&
                    r.InternalNotes != null &&
                    r.InternalNotes.Contains(transactionId.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    r.InternalNotes.Contains("Checkout session expired", StringComparison.OrdinalIgnoreCase)),
                "System-PaymentService",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PaymentFailedEventConsumerWithOrderServiceRoutingCancelsOrder()
        {
            var consumer = new PaymentFailedEventConsumer(_statusServiceMock.Object, _paymentFailedLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<PaymentFailedEvent>>();
            var transactionId = Guid.NewGuid();

            _ = contextMock.Setup(c => c.Message).Returns(new PaymentFailedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new PaymentFailedEventPayload
                {
                    TransactionId = transactionId,
                    IdempotencyKey = "payment-failed",
                    Amount = 100,
                    Currency = "THB",
                    CustomerId = "customer-1",
                    OrderId = "ORD-FAILED-1",
                    ProviderName = "stripe",
                    ErrorMessage = "Card was declined",
                    ProviderErrorCode = "card_declined",
                    FailedAt = DateTimeOffset.UtcNow
                }
            });

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                    It.IsAny<string>(),
                    It.IsAny<CreateOrderStatusRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderStatusResponse
                {
                    StatusId = 1,
                    OrderId = "ORD-FAILED-1",
                    Status = "Cancelled",
                    UpdatedBy = "System-PaymentService",
                    Timestamp = DateTime.UtcNow
                });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                "ORD-FAILED-1",
                It.Is<CreateOrderStatusRequest>(r =>
                    r.Status == "Cancelled" &&
                    r.InternalNotes != null &&
                    r.InternalNotes.Contains(transactionId.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    r.InternalNotes.Contains("Card was declined", StringComparison.OrdinalIgnoreCase) &&
                    r.InternalNotes.Contains("card_declined", StringComparison.OrdinalIgnoreCase) &&
                    r.CustomerNotes == "Payment failed before completion."),
                "System-PaymentService",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerCompletedStatusUpdatesOrderToFinished()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();
            var jobId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            const string orderNumber = "ORD-2026-00456";

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new JobStatusChangedEventPayload
                {
                    JobId = jobId,
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    PreviousStatus = "Finishing",
                    NewStatus = "Completed",
                    Technology = "FDM",
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = "scanner-operator"
                }
            });

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                orderNumber,
                It.IsAny<CreateOrderStatusRequest>(),
                "System-JobService",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderStatusResponse
                {
                    StatusId = 2,
                    OrderId = orderNumber,
                    Status = "Finished",
                    UpdatedBy = "System-JobService",
                    Timestamp = DateTime.UtcNow
                });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                orderNumber,
                It.Is<CreateOrderStatusRequest>(r =>
                    r.Status == "Finished" &&
                    r.InternalNotes != null &&
                    r.InternalNotes.Contains(jobId.ToString(), StringComparison.Ordinal)),
                "System-JobService",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerCompletedStatusWithoutOrderServiceRoutingIsIgnored()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();
            var jobId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            const string orderNumber = "ORD-2026-00458";

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["NotificationService"],
                Payload = new JobStatusChangedEventPayload
                {
                    JobId = jobId,
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    PreviousStatus = "Finishing",
                    NewStatus = "Completed",
                    Technology = "FDM",
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = "scanner-operator"
                }
            });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerWithoutPayloadIsIgnored()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = null!
            });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerInProgressStatusUpdatesOrderToInProgress()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();
            var jobId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            const string orderNumber = "ORD-2026-00459";

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new JobStatusChangedEventPayload
                {
                    JobId = jobId,
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    PreviousStatus = "Queued",
                    NewStatus = "InProgress",
                    Technology = "FDM",
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = "scanner-operator"
                }
            });

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                orderNumber,
                It.IsAny<CreateOrderStatusRequest>(),
                "System-JobService",
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OrderStatusResponse
                {
                    StatusId = 2,
                    OrderId = orderNumber,
                    Status = "InProgress",
                    UpdatedBy = "System-JobService",
                    Timestamp = DateTime.UtcNow
                });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                orderNumber,
                It.Is<CreateOrderStatusRequest>(r =>
                    r.Status == "InProgress" &&
                    r.InternalNotes != null &&
                    r.InternalNotes.Contains(jobId.ToString(), StringComparison.Ordinal) &&
                    r.CustomerNotes != null &&
                    r.CustomerNotes.Contains("Production has started", StringComparison.Ordinal)),
                "System-JobService",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerUnhandledStatusDoesNotUpdateOrder()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new JobStatusChangedEventPayload
                {
                    JobId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    OrderNumber = "ORD-2026-00457",
                    PreviousStatus = "InProgress",
                    NewStatus = "Finishing",
                    Technology = "FDM",
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = "scanner-operator"
                }
            });

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.CreateOrderStatusAsync(
                It.IsAny<string>(),
                It.IsAny<CreateOrderStatusRequest>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerDuplicateInProgressStatusDoesNotThrow()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();
            const string orderNumber = "ORD-2026-00460";

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new JobStatusChangedEventPayload
                {
                    JobId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    PreviousStatus = "Queued",
                    NewStatus = "InProgress",
                    Technology = "FDM",
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = "scanner-operator"
                }
            });

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                orderNumber,
                It.IsAny<CreateOrderStatusRequest>(),
                "System-JobService",
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Invalid transition from InProgress to InProgress"));

            _ = _statusServiceMock.Setup(s => s.GetOrderStatusHistoryAsync(orderNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new()
                    {
                        StatusId = 3,
                        OrderId = orderNumber,
                        Status = "InProgress",
                        UpdatedBy = "System-JobService",
                        Timestamp = DateTime.UtcNow
                    }
                ]);

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.GetOrderStatusHistoryAsync(orderNumber, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerCompletedStatusWithoutOrderNumberThrows()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new JobStatusChangedEventPayload
                {
                    JobId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    PreviousStatus = "Finishing",
                    NewStatus = "Completed",
                    Technology = "FDM",
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = "scanner-operator"
                }
            });

            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(contextMock.Object));
        }

        [Fact]
        public async Task JobStatusChangedEventConsumerDuplicateFinishedStatusDoesNotThrow()
        {
            var consumer = new JobStatusChangedEventConsumer(_statusServiceMock.Object, _jobStatusLoggerMock.Object);
            var contextMock = new Mock<ConsumeContext<JobStatusChangedEvent>>();
            const string orderNumber = "ORD-2026-00458";

            _ = contextMock.Setup(c => c.Message).Returns(new JobStatusChangedEvent
            {
                ConsumedBy = ["OrderService"],
                Payload = new JobStatusChangedEventPayload
                {
                    JobId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    PreviousStatus = "Finishing",
                    NewStatus = "Completed",
                    Technology = "FDM",
                    ChangedAt = DateTimeOffset.UtcNow,
                    ChangedBy = "scanner-operator"
                }
            });

            _ = _statusServiceMock.Setup(s => s.CreateOrderStatusAsync(
                orderNumber,
                It.IsAny<CreateOrderStatusRequest>(),
                "System-JobService",
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Invalid transition from Finished to Finished"));

            _ = _statusServiceMock.Setup(s => s.GetOrderStatusHistoryAsync(orderNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new()
                    {
                        StatusId = 3,
                        OrderId = orderNumber,
                        Status = "Finished",
                        UpdatedBy = "System-JobService",
                        Timestamp = DateTime.UtcNow
                    }
                ]);

            await consumer.Consume(contextMock.Object);

            _statusServiceMock.Verify(s => s.GetOrderStatusHistoryAsync(orderNumber, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

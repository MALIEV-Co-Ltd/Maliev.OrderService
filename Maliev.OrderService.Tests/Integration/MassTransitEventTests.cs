using System.Net;
using System.Net.Http.Json;
using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.MessagingContracts.Contracts.Payments;
using Maliev.OrderService.Api.Consumers;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Tests.Testing;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.OrderService.Tests.Integration
{
    /// <summary>
    /// Integration tests for MassTransit event publishing and consuming
    /// </summary>
    public class MassTransitEventTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MassTransitEventTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateAuthenticatedClient(
                userId: "test-admin",
                roles: ["admin"],
                permissions: [
                    "order.orders.create",
                    "order.orders.read",
                    "order.orders.update",
                    "order.orders.approve",
                    "order.orders.cancel",
                    "order.orders.fulfill"
                ]);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _factory.ResetDatabaseAsync();
            _client.Dispose();
        }

        [Fact]
        public async Task CreateOrderShouldPublishOrderCreatedEvent()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-001",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 10,
                Requirements = "Test order for event publishing"
            };

            // Get the test harness
            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {
                // Act
                HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                // Verify OrderCreatedEvent was published
                Assert.True(await harness.Published.Any<OrderCreatedEvent>(), "OrderCreatedEvent should be published");

                // Get the published message
                IPublishedMessage<OrderCreatedEvent>? publishedMessage = harness.Published.Select<OrderCreatedEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderCreatedEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderCreatedEvent", @event.MessageName);
                Assert.Equal("OrderService", @event.PublishedBy);
                Assert.Equal(MessageType.Event, @event.MessageType);
                Assert.NotNull(@event.Payload);
                Assert.NotEqual(Guid.Empty, @event.Payload.OrderId);
                Assert.Contains("ORD-", @event.Payload.OrderNumber);
                Assert.Equal("THB", @event.Payload.Currency);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [Fact]
        public async Task UpdateOrderStatusShouldPublishOrderStatusChangedEvent()
        {
            // Arrange
            // First create an order
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-002",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 5,
                Requirements = "Test order for status change events"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {
                // Act - Update status to "Reviewing"
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Reviewing",
                    InternalNotes = "Starting review process"
                };

                HttpResponseMessage statusResponse = await _client.PostAsJsonAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    statusRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, statusResponse.StatusCode);

                // Verify OrderStatusChangedEvent was published
                Assert.True(await harness.Published.Any<OrderStatusChangedEvent>(),
                    "OrderStatusChangedEvent should be published");

                IPublishedMessage<OrderStatusChangedEvent>? publishedMessage = harness.Published.Select<OrderStatusChangedEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderStatusChangedEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderStatusChangedEvent", @event.MessageName);
                Assert.Equal(MessageType.Event, @event.MessageType);
                Assert.NotNull(@event.Payload);
                Assert.Equal("New", @event.Payload.PreviousStatus);
                Assert.Equal("Reviewing", @event.Payload.NewStatus);
                Assert.Contains("QuoteEngine", @event.ConsumedBy);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [Fact]
        public async Task UpdateStatusToQuotedShouldPublishOrderQuotedEvent()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-003",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 3,
                Requirements = "Test order for quoted event"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            // Transition to Reviewed first
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewing" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewed" });

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {

                // Act - Update status to "Quoted"
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Quoted",
                    InternalNotes = "Quote prepared"
                };

                HttpResponseMessage statusResponse = await _client.PostAsJsonAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    statusRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, statusResponse.StatusCode);

                // Verify OrderQuotedEvent was published
                Assert.True(await harness.Published.Any<OrderQuotedEvent>(),
                    "OrderQuotedEvent should be published");

                IPublishedMessage<OrderQuotedEvent>? publishedMessage = harness.Published.Select<OrderQuotedEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderQuotedEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderQuotedEvent", @event.MessageName);
                Assert.NotNull(@event.Payload);
                Assert.NotEqual(Guid.Empty, @event.Payload.OrderId);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [Fact]
        public async Task UpdateStatusToAcceptedShouldPublishOrderAcceptedEvent()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-004",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 2,
                Requirements = "Test order for accepted event"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            // Transition through states to Quoted
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewing" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewed" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Quoted" });

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {

                // Act - Update status to "Accepted"
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Accepted",
                    CustomerNotes = "Quote accepted by customer"
                };

                HttpResponseMessage statusResponse = await _client.PostAsJsonAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    statusRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, statusResponse.StatusCode);

                // Verify OrderAcceptedEvent was published
                Assert.True(await harness.Published.Any<OrderAcceptedEvent>(),
                    "OrderAcceptedEvent should be published");

                IPublishedMessage<OrderAcceptedEvent>? publishedMessage = harness.Published.Select<OrderAcceptedEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderAcceptedEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderAcceptedEvent", @event.MessageName);
                Assert.NotNull(@event.Payload);
                Assert.NotEqual(Guid.Empty, @event.Payload.CustomerId);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [Fact]
        public async Task UpdateStatusToCancelledShouldPublishOrderCancelledEvent()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-005",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 1,
                Requirements = "Test order for cancelled event"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {

                // Act - Cancel the order
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Cancelled",
                    InternalNotes = "Customer requested cancellation"
                };

                HttpResponseMessage statusResponse = await _client.PostAsJsonAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    statusRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, statusResponse.StatusCode);

                // Verify OrderCancelledEvent was published
                Assert.True(await harness.Published.Any<OrderCancelledEvent>(),
                    "OrderCancelledEvent should be published");

                IPublishedMessage<OrderCancelledEvent>? publishedMessage = harness.Published.Select<OrderCancelledEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderCancelledEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderCancelledEvent", @event.MessageName);
                Assert.NotNull(@event.Payload);
                Assert.False(@event.Payload.RefundRequired); // Not paid yet
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is used for deterministic hashing in tests, not cryptography")]
        [Fact]
        public async Task PaymentCompletedEventConsumerShouldUpdateOrderToPaidStatus()
        {
            // Arrange
            // Create an order and accept it
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-006",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 5,
                Requirements = "Test order for payment completed consumer"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            // Transition to Accepted
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewing" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewed" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Quoted" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Accepted" });

            // Verify it's actually Accepted before proceeding
            List<OrderStatusResponse>? history = await _client.GetFromJsonAsync<List<OrderStatusResponse>>($"/order/v1/orders/{createdOrder.OrderId}/statuses");
            Assert.Contains(history!, s => s.Status == "Accepted");

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {

                // Convert OrderId string to Guid for the event
                byte[] hash = System.Security.Cryptography.MD5.HashData(
                    System.Text.Encoding.UTF8.GetBytes(createdOrder.OrderId));
                Guid orderGuid = new(hash);

                // Act - Publish PaymentCompletedEvent
                var paymentId = Guid.NewGuid();
                PaymentCompletedEvent paymentCompletedEvent = new(
                    MessageId: Guid.NewGuid(),
                    MessageName: "PaymentCompletedEvent",
                    MessageType: MessageType.Event,
                    MessageVersion: "1.0.0",
                    PublishedBy: "PaymentService",
                    ConsumedBy: ["OrderService"],
                    CorrelationId: Guid.NewGuid(),
                    CausationId: null,
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    IsPublic: false,
                    Payload: new PaymentCompletedEventPayload(
                        OrderId: orderGuid,
                        OrderNumber: createdOrder.OrderId,
                        CustomerId: createdOrder.CustomerId,
                        PaymentId: paymentId,
                        Amount: 1500.00,
                        Currency: "THB"
                    )
                    {
                        ProviderName = "omise"
                    }
                );

                IConsumerTestHarness<PaymentCompletedEventConsumer> consumerHarness = harness.GetConsumerHarness<PaymentCompletedEventConsumer>();
                await harness.Bus.Publish(paymentCompletedEvent);

                bool consumed = await TestHelpers.WaitForAsync(
                    async ct =>
                        await consumerHarness.Consumed.Any<PaymentCompletedEvent>(
                            x => x.Context.Message.Payload.OrderNumber == createdOrder.OrderId,
                            ct)
                        || await harness.Consumed.Any<PaymentCompletedEvent>(ct),
                    static result => result,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromMilliseconds(100),
                    $"PaymentCompletedEvent should be consumed. Expected OrderNumber: {createdOrder.OrderId}");

                Assert.True(consumed, $"PaymentCompletedEvent should be consumed. Expected OrderNumber: {createdOrder.OrderId}");

                List<OrderStatusResponse>? statuses = await TestHelpers.WaitForAsync(
                    async ct =>
                    {
                        HttpResponseMessage statusHistoryResponse = await _client.GetAsync(
                            $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                            ct);
                        Assert.Equal(HttpStatusCode.OK, statusHistoryResponse.StatusCode);
                        return await statusHistoryResponse.Content.ReadFromJsonAsync<List<OrderStatusResponse>>(cancellationToken: ct);
                    },
                    statusHistory => statusHistory?.Any(s => s.Status == "Paid") == true,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromMilliseconds(100),
                    $"Paid status should be persisted for order {createdOrder.OrderId}");

                Assert.NotNull(statuses);
                Assert.Contains(statuses, s => s.Status == "Paid");

                // Verify the Paid status has the payment information in notes
                OrderStatusResponse? paidStatus = statuses.FirstOrDefault(s => s.Status == "Paid");
                Assert.NotNull(paidStatus);
                Assert.Contains("Payment", paidStatus.InternalNotes ?? "");
                Assert.Contains("completed", paidStatus.InternalNotes ?? "");

                // Verify OrderPaidEvent was also published
                Assert.True(await harness.Published.Any<OrderPaidEvent>(),
                    "OrderPaidEvent should be published after consuming PaymentCompletedEvent");

                IPublishedMessage<OrderPaidEvent>? orderPaidEvent = harness.Published.Select<OrderPaidEvent>().FirstOrDefault();
                Assert.NotNull(orderPaidEvent);
                Assert.Equal(paymentId, orderPaidEvent.Context.Message.Payload.PaymentId);
                Assert.Equal(1500.00, orderPaidEvent.Context.Message.Payload.PaidAmount);
                Assert.Equal("THB", orderPaidEvent.Context.Message.Payload.Currency);
                Assert.Equal("omise", orderPaidEvent.Context.Message.Payload.ProviderName);

                OrderResponse? paidOrder = await _client.GetFromJsonAsync<OrderResponse>($"/order/v1/orders/{createdOrder.OrderId}");
                Assert.NotNull(paidOrder);
                Assert.Equal(paymentId.ToString(), paidOrder.PaymentId);
                Assert.Equal("Paid", paidOrder.PaymentStatus);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [Fact]
        public async Task PaymentFailedEventConsumerShouldCancelAcceptedOrder()
        {
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-FAILED-001",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 2,
                Requirements = "Test order for payment failure consumer"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewing" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewed" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Quoted" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Accepted" });

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();
            IConsumerTestHarness<PaymentFailedEventConsumer> consumerHarness = harness.GetConsumerHarness<PaymentFailedEventConsumer>();
            var transactionId = Guid.NewGuid();
            var paymentFailedEvent = new PaymentFailedEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(PaymentFailedEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "PaymentService",
                ConsumedBy: ["OrderService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new PaymentFailedEventPayload(
                    TransactionId: transactionId,
                    IdempotencyKey: "stripe-failure",
                    Amount: 1500.00,
                    Currency: "THB",
                    CustomerId: createdOrder.CustomerId,
                    OrderId: createdOrder.OrderId,
                    ProviderName: "stripe",
                    ErrorMessage: "Card was declined",
                    ProviderErrorCode: "card_declined",
                    FailedAt: DateTimeOffset.UtcNow));

            await harness.Bus.Publish(paymentFailedEvent);

            bool consumed = await TestHelpers.WaitForAsync(
                async ct =>
                    await consumerHarness.Consumed.Any<PaymentFailedEvent>(
                        x => x.Context.Message.Payload.OrderId == createdOrder.OrderId,
                        ct),
                static result => result,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(100),
                $"PaymentFailedEvent should be consumed. Expected OrderId: {createdOrder.OrderId}");

            Assert.True(consumed, $"PaymentFailedEvent should be consumed. Expected OrderId: {createdOrder.OrderId}");

            List<OrderStatusResponse>? statuses = await TestHelpers.WaitForAsync(
                async ct =>
                {
                    HttpResponseMessage statusHistoryResponse = await _client.GetAsync(
                        $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                        ct);
                    Assert.Equal(HttpStatusCode.OK, statusHistoryResponse.StatusCode);
                    return await statusHistoryResponse.Content.ReadFromJsonAsync<List<OrderStatusResponse>>(cancellationToken: ct);
                },
                statusHistory => statusHistory?.Any(s => s.Status == "Cancelled") == true,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(100),
                $"Cancelled status should be persisted for order {createdOrder.OrderId}");

            Assert.NotNull(statuses);
            OrderStatusResponse cancelledStatus = Assert.Single(statuses, s => s.Status == "Cancelled");
            Assert.Contains(transactionId.ToString(), cancelledStatus.InternalNotes ?? string.Empty);
            Assert.Contains("Card was declined", cancelledStatus.InternalNotes ?? string.Empty);
            Assert.Contains("card_declined", cancelledStatus.InternalNotes ?? string.Empty);
        }

        [Fact]
        public async Task UpdateStatusToPaidShouldPublishOrderPaidEvent()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-007",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 4,
                Requirements = "Test order for paid event"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            // Transition to Accepted
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewing" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewed" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Quoted" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Accepted" });

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {

                // Act - Mark as Paid
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Paid",
                    InternalNotes = "Payment received"
                };

                HttpResponseMessage statusResponse = await _client.PostAsJsonAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    statusRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, statusResponse.StatusCode);

                // Verify OrderPaidEvent was published
                Assert.True(await harness.Published.Any<OrderPaidEvent>(),
                    "OrderPaidEvent should be published");

                IPublishedMessage<OrderPaidEvent>? publishedMessage = harness.Published.Select<OrderPaidEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderPaidEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderPaidEvent", @event.MessageName);
                Assert.Contains("JobService", @event.ConsumedBy);
                Assert.NotNull(@event.Payload);
                Assert.Equal("THB", @event.Payload.Currency);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [Fact]
        public async Task UpdateStatusToInProgressShouldPublishOrderInProgressEvent()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-008",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 6,
                Requirements = "Test order for in progress event"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            // Transition to Paid
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewing" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewed" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Quoted" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Accepted" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Paid" });

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {

                // Act - Start production
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "InProgress",
                    InternalNotes = "Production started"
                };

                HttpResponseMessage statusResponse = await _client.PostAsJsonAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    statusRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, statusResponse.StatusCode);

                // Verify OrderInProgressEvent was published
                Assert.True(await harness.Published.Any<OrderInProgressEvent>(),
                    "OrderInProgressEvent should be published");

                IPublishedMessage<OrderInProgressEvent>? publishedMessage = harness.Published.Select<OrderInProgressEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderInProgressEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderInProgressEvent", @event.MessageName);
                Assert.NotNull(@event.Payload);
                Assert.NotEqual(default, @event.Payload.StartedAt);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }

        [Fact]
        public async Task UpdateStatusToQualityReleasedShouldPublishOrderCompletedEvent()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-009",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 7,
                Requirements = "Test order for completed event"
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            OrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
            Assert.NotNull(createdOrder);

            // Transition to InProgress
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewing" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Reviewed" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Quoted" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Accepted" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "Paid" });
            _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                new CreateOrderStatusRequest { Status = "InProgress" });

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {

                _ = await _client.PostAsJsonAsync($"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    new CreateOrderStatusRequest { Status = "Finished", InternalNotes = "Production completed" });

                // Act - Release the order from QC
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "QualityReleased",
                    InternalNotes = "QC released for shipping"
                };

                HttpResponseMessage statusResponse = await _client.PostAsJsonAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses",
                    statusRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, statusResponse.StatusCode);

                // Verify OrderCompletedEvent was published
                Assert.True(await harness.Published.Any<OrderCompletedEvent>(),
                    "OrderCompletedEvent should be published");

                IPublishedMessage<OrderCompletedEvent>? publishedMessage = harness.Published.Select<OrderCompletedEvent>().FirstOrDefault();
                Assert.NotNull(publishedMessage);

                OrderCompletedEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderCompletedEvent", @event.MessageName);
                Assert.Contains("DeliveryService", @event.ConsumedBy);
                Assert.NotNull(@event.Payload);
                Assert.NotEqual(Guid.Empty, @event.Payload.CustomerId);
                Assert.NotEqual(default, @event.Payload.CompletedAt);
            }
            finally
            {
                // No need to stop harness manually here
            }
        }
    }
}

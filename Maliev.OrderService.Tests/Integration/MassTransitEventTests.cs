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
            var sourceProjectId = Guid.NewGuid();
            var sourceProjectPartId = Guid.NewGuid();
            var materialId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var createRequest = new CreateOrderRequest
            {
                CustomerId = customerId.ToString("D"),
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 10,
                Requirements = "Test order for event publishing",
                QuotedAmount = 1000m,
                QuoteCurrency = "THB",
                ProductionItems =
                [
                    new CreateOrderProductionItemRequest
                    {
                        SourceProjectId = sourceProjectId,
                        SourceProjectPartId = sourceProjectPartId,
                        MaterialId = materialId,
                        MaterialSnapshotJson = "{}",
                        ConfigurationSnapshotJson = "{}",
                        Technology = "FDM",
                        Quantity = 10
                    }
                ]
            };

            // Get the test harness
            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();

            try
            {
                // Act
                HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                IPublishedMessage<OrderCreatedEvent> publishedMessage = await WaitForPublishedAsync<OrderCreatedEvent>(
                    harness,
                    message => message.Payload.CustomerId == customerId,
                    "OrderCreatedEvent should be published for the created customer");

                OrderCreatedEvent @event = publishedMessage.Context.Message;
                Assert.Equal("OrderCreatedEvent", @event.MessageName);
                Assert.Equal("OrderService", @event.PublishedBy);
                Assert.Equal(MessageType.Event, @event.MessageType);
                Assert.NotNull(@event.Payload);
                Assert.NotEqual(Guid.Empty, @event.Payload.OrderId);
                Assert.Equal(customerId, @event.Payload.CustomerId);
                Assert.Contains("ORD-", @event.Payload.OrderNumber);
                Assert.Equal("THB", @event.Payload.Currency);
                Assert.Contains("ProjectService", @event.ConsumedBy);
                OrderCreatedEventPayloadItemsItem item = Assert.Single(@event.Payload.Items);
                Assert.Equal(sourceProjectId, item.SourceProjectId);
                Assert.Equal(sourceProjectPartId, item.SourceProjectPartId);
                Assert.NotEqual(Guid.Empty, item.ProductId);
                Assert.Equal(10, item.Quantity);
                Assert.Equal(1000, item.LineTotal);
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

                IPublishedMessage<OrderStatusChangedEvent> publishedMessage = await WaitForPublishedAsync<OrderStatusChangedEvent>(
                    harness,
                    message => message.Payload.OrderNumber == createdOrder.OrderId && message.Payload.NewStatus == "Reviewing",
                    "OrderStatusChangedEvent should be published for the created order");

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

                IPublishedMessage<OrderQuotedEvent> publishedMessage = await WaitForPublishedAsync<OrderQuotedEvent>(
                    harness,
                    message => message.Payload.OrderNumber == createdOrder.OrderId,
                    "OrderQuotedEvent should be published for the created order");

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

                IPublishedMessage<OrderAcceptedEvent> publishedMessage = await WaitForPublishedAsync<OrderAcceptedEvent>(
                    harness,
                    message => message.Payload.OrderNumber == createdOrder.OrderId,
                    "OrderAcceptedEvent should be published for the created order");

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

                IPublishedMessage<OrderCancelledEvent> publishedMessage = await WaitForPublishedAsync<OrderCancelledEvent>(
                    harness,
                    message => message.Payload.OrderNumber == createdOrder.OrderId,
                    "OrderCancelledEvent should be published for the created order");

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

                IPublishedMessage<OrderPaidEvent> orderPaidEvent = await WaitForPublishedAsync<OrderPaidEvent>(
                    harness,
                    message => message.Payload.PaymentId == paymentId,
                    "OrderPaidEvent should be published for the completed payment");
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
        public async Task PaymentPendingEventConsumerShouldMarkAcceptedOrderPaymentProcessing()
        {
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "CUST-PENDING-001",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                OrderedQuantity = 2,
                Requirements = "Test order for payment pending consumer"
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
            IConsumerTestHarness<PaymentPendingEventConsumer> consumerHarness = harness.GetConsumerHarness<PaymentPendingEventConsumer>();
            var transactionId = Guid.NewGuid();
            var paymentPendingEvent = new PaymentPendingEvent(
                MessageId: Guid.NewGuid(),
                MessageName: nameof(PaymentPendingEvent),
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "PaymentService",
                ConsumedBy: ["OrderService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new PaymentPendingEventPayload(
                    TransactionId: transactionId,
                    IdempotencyKey: "stripe-pending",
                    Amount: 1500.00,
                    Currency: "THB",
                    CustomerId: createdOrder.CustomerId,
                    OrderId: createdOrder.OrderId,
                    ProviderName: "stripe",
                    ProviderEventCode: "ProviderSuccess",
                    PendingAt: DateTimeOffset.UtcNow));

            await harness.Bus.Publish(paymentPendingEvent);

            bool consumed = await TestHelpers.WaitForAsync(
                async ct =>
                    await consumerHarness.Consumed.Any<PaymentPendingEvent>(
                        x => x.Context.Message.Payload.OrderId == createdOrder.OrderId,
                        ct),
                static result => result,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(100),
                $"PaymentPendingEvent should be consumed. Expected OrderId: {createdOrder.OrderId}");

            Assert.True(consumed, $"PaymentPendingEvent should be consumed. Expected OrderId: {createdOrder.OrderId}");

            OrderResponse? pendingOrder = await TestHelpers.WaitForAsync(
                async ct => await _client.GetFromJsonAsync<OrderResponse>(
                    $"/order/v1/orders/{createdOrder.OrderId}",
                    ct),
                order => order?.PaymentStatus == "Processing" && order.PaymentId == transactionId.ToString(),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(100),
                $"Order payment state should be Processing for order {createdOrder.OrderId}");

            Assert.NotNull(pendingOrder);
            Assert.Equal("Processing", pendingOrder.PaymentStatus);
            Assert.Equal(transactionId.ToString(), pendingOrder.PaymentId);

            List<OrderStatusResponse>? statuses = await _client.GetFromJsonAsync<List<OrderStatusResponse>>($"/order/v1/orders/{createdOrder.OrderId}/statuses");
            Assert.NotNull(statuses);
            Assert.DoesNotContain(statuses, status => status.Status == "Paid");
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

                IPublishedMessage<OrderPaidEvent> publishedMessage = await WaitForPublishedAsync<OrderPaidEvent>(
                    harness,
                    message => message.Payload.OrderNumber == createdOrder.OrderId,
                    "OrderPaidEvent should be published for the created order");

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

                IPublishedMessage<OrderInProgressEvent> publishedMessage = await WaitForPublishedAsync<OrderInProgressEvent>(
                    harness,
                    message => message.Payload.OrderNumber == createdOrder.OrderId,
                    "OrderInProgressEvent should be published for the created order");

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

                IPublishedMessage<OrderCompletedEvent> publishedMessage = await WaitForPublishedAsync<OrderCompletedEvent>(
                    harness,
                    message => message.Payload.OrderNumber == createdOrder.OrderId,
                    "OrderCompletedEvent should be published for the created order");

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

        private static async Task<IPublishedMessage<TMessage>> WaitForPublishedAsync<TMessage>(
            ITestHarness harness,
            Func<TMessage, bool> predicate,
            string failureMessage)
            where TMessage : class
        {
            IPublishedMessage<TMessage>? publishedMessage = await TestHelpers.WaitForAsync(
                cancellationToken => Task.FromResult(
                    harness.Published
                        .Select<TMessage>(cancellationToken)
                        .FirstOrDefault(message => predicate(message.Context.Message))),
                static message => message is not null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(100),
                failureMessage);

            return Assert.IsAssignableFrom<IPublishedMessage<TMessage>>(publishedMessage);
        }
    }
}

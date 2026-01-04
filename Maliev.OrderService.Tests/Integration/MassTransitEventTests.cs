using System.Net;
using System.Net.Http.Json;
using Maliev.MessagingContracts.Generated;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
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
        public async Task CreateOrder_ShouldPublishOrderCreatedEvent()
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
            await harness.Start();

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
                await harness.Stop();
            }
        }

        [Fact]
        public async Task UpdateOrderStatus_ShouldPublishOrderStatusChangedEvent()
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
            await harness.Start();

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
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Fact]
        public async Task UpdateStatusToQuoted_ShouldPublishOrderQuotedEvent()
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
            await harness.Start();

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
                await harness.Stop();
            }
        }

        [Fact]
        public async Task UpdateStatusToAccepted_ShouldPublishOrderAcceptedEvent()
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
            await harness.Start();

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
                await harness.Stop();
            }
        }

        [Fact]
        public async Task UpdateStatusToCancelled_ShouldPublishOrderCancelledEvent()
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
            await harness.Start();

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
                await harness.Stop();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is used for deterministic hashing in tests, not cryptography")]
        [Fact]
        public async Task PaymentCompletedEventConsumer_ShouldUpdateOrderToPaidStatus()
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

            ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();
            await harness.Start();

            try
            {

                // Convert OrderId string to Guid for the event
                byte[] hash = System.Security.Cryptography.MD5.HashData(
                    System.Text.Encoding.UTF8.GetBytes(createdOrder.OrderId));
                Guid orderGuid = new(hash);

                // Act - Publish PaymentCompletedEvent
                var paymentCompletedEvent = new PaymentCompletedEvent(
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
                        PaymentId: Guid.NewGuid(),
                        Amount: 1500.00,
                        Currency: "THB"
                    )
                );

                await harness.Bus.Publish(paymentCompletedEvent);

                // Wait for consumer to process
                Assert.True(await harness.Consumed.Any<PaymentCompletedEvent>(),
                    "PaymentCompletedEvent should be consumed");

                // Give some time for async processing
                await Task.Delay(500);

                // Assert - Verify order status was updated to "Paid"
                HttpResponseMessage statusHistoryResponse = await _client.GetAsync(
                    $"/order/v1/orders/{createdOrder.OrderId}/statuses");
                Assert.Equal(HttpStatusCode.OK, statusHistoryResponse.StatusCode);

                List<OrderStatusResponse>? statuses = await statusHistoryResponse.Content.ReadFromJsonAsync<List<OrderStatusResponse>>();
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
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Fact]
        public async Task UpdateStatusToPaid_ShouldPublishOrderPaidEvent()
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
            await harness.Start();

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
                Assert.NotNull(@event.Payload);
                Assert.Equal("THB", @event.Payload.Currency);
            }
            finally
            {
                await harness.Stop();
            }
        }

        [Fact]
        public async Task UpdateStatusToInProgress_ShouldPublishOrderInProgressEvent()
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
            await harness.Start();

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
                await harness.Stop();
            }
        }

        [Fact]
        public async Task UpdateStatusToFinished_ShouldPublishOrderCompletedEvent()
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
            await harness.Start();

            try
            {

                // Act - Complete the order
                var statusRequest = new CreateOrderStatusRequest
                {
                    Status = "Finished",
                    InternalNotes = "Production completed"
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
                Assert.NotNull(@event.Payload);
                Assert.NotEqual(default, @event.Payload.CompletedAt);
            }
            finally
            {
                await harness.Stop();
            }
        }
    }
}

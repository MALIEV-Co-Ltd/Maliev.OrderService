using System.Net;
using System.Net.Http.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class FulfillmentAccessTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private static readonly string[] FulfillmentRoles = ["roles.order.fulfillment"];
        private static readonly string[] FulfillmentPermissions = [OrderPermissions.OrdersRead, OrderPermissions.OrdersUpdate, OrderPermissions.OrdersFulfill];
        private static readonly string[] UpdateOnlyPermissions = [OrderPermissions.OrdersUpdate];

        [Fact]
        public async Task FulfillmentCanMarkAsFulfilled()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient("fulfillment-user",
                roles: FulfillmentRoles,
                permissions: FulfillmentPermissions);

            string orderId = await CreateTestOrderAsync("InProgress");

            var request = new { Status = "Finished", InternalNotes = "Order completed" };

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task FulfillmentCannotUpdatePricing()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient("fulfillment-user",
                roles: FulfillmentRoles,
                permissions: UpdateOnlyPermissions); // Has update but not approve

            string orderId = await CreateTestOrderAsync();

            var request = new
            {
                Version = "1",
                QuotedAmount = 1000.00m // Unauthorized field
            };

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync($"/order/v1/orders/{orderId}", request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<string> CreateTestOrderAsync(string initialStatus = "New")
        {
            using OrderDbContext context = _factory.CreateDbContext();
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerId = "CUST-001",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                AssignedEmployeeId = "fulfillment-user",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                UpdatedBy = "system"
            };
            _ = context.Orders.Add(order);

            _ = context.OrderStatuses.Add(new OrderStatus
            {
                OrderId = order.OrderId,
                Status = initialStatus,
                Timestamp = DateTime.UtcNow,
                UpdatedBy = "system"
            });

            _ = await context.SaveChangesAsync();
            return order.OrderId;
        }
    }
}

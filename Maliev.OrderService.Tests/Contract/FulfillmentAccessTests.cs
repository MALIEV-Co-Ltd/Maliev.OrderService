using System.Net;
using System.Net.Http.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class FulfillmentAccessTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private static readonly string[] FulfillmentRoles = ["roles.order.fulfillment"];
        private static readonly string[] FulfillmentPermissions = [OrderPermissions.OrdersRead, OrderPermissions.OrdersUpdate, OrderPermissions.OrdersFulfill];
        private static readonly string[] UpdateOnlyPermissions = [OrderPermissions.OrdersUpdate];

        [Fact]
        public async Task Fulfillment_CanMarkAsFulfilled()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient("fulfillment-user",
                roles: FulfillmentRoles,
                permissions: FulfillmentPermissions);

            var orderId = await CreateTestOrderAsync();

            var request = new { Status = "Finished", InternalNotes = "Order completed" };

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Fulfillment_CannotUpdatePricing()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient("fulfillment-user",
                roles: FulfillmentRoles,
                permissions: UpdateOnlyPermissions); // Has update but not approve

            var orderId = await CreateTestOrderAsync();

            var request = new
            {
                Version = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                QuotedAmount = 1000.00m // Unauthorized field
            };

            // Act
            HttpResponseMessage response = await client.PutAsJsonAsync($"/order/v1/orders/{orderId}", request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<string> CreateTestOrderAsync()
        {
            using OrderDbContext context = _factory.CreateDbContext();
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerId = "CUST-001",
                CustomerType = "Customer",
                ServiceCategoryId = 1,
                ProcessTypeId = 1,
                CreatedAt = DateTime.UtcNow,
                Version = Guid.NewGuid().ToByteArray(),
                CreatedBy = "system",
                UpdatedBy = "system"
            };
            _ = context.Orders.Add(order);
            _ = await context.SaveChangesAsync();
            return order.OrderId;
        }
    }
}

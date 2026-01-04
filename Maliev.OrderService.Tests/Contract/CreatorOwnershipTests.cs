using System.Net;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Data;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class CreatorOwnershipTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private static readonly string[] ReadPermissions = [OrderPermissions.OrdersRead];
        private static readonly string[] CreatorRoles = ["roles.order.creator"];

        [Fact]
        public async Task Creator_CanReadOwnOrder()
        {
            // Arrange
            string userId = "creator-001";
            HttpClient client = _factory.CreateAuthenticatedClient(userId,
                roles: CreatorRoles,
                permissions: ReadPermissions);

            string orderId = await CreateTestOrderAsync(userId);

            // Act
            HttpResponseMessage response = await client.GetAsync($"/order/v1/orders/{orderId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Creator_CannotReadOthersOrder()
        {
            // Arrange
            string creator1 = "creator-001";
            string creator2 = "creator-002";

            HttpClient client = _factory.CreateAuthenticatedClient(creator1,
                roles: CreatorRoles,
                permissions: ReadPermissions);

            string othersOrderId = await CreateTestOrderAsync(creator2);

            // Act
            HttpResponseMessage response = await client.GetAsync($"/order/v1/orders/{othersOrderId}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<string> CreateTestOrderAsync(string customerId)
        {
            using OrderDbContext context = _factory.CreateDbContext();
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                CustomerId = customerId,
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

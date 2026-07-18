using System.Net;
using System.Net.Http.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class ManagerAccessTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;

        [Fact]
        public async Task ManagerCanCreateOrder()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient("manager-user",
                permissions: [OrderPermissions.OrdersCreate]);

            var request = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/order/v1/orders", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task ManagerCannotDeleteOrder()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient("manager-user",
                permissions: [OrderPermissions.OrdersRead]); // No delete permission

            string orderId = await CreateTestOrderAsync();

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/order/v1/orders/{orderId}");

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
                CreatedBy = "system",
                UpdatedBy = "system"
            };
            _ = context.Orders.Add(order);
            _ = await context.SaveChangesAsync();
            return order.OrderId;
        }
    }
}

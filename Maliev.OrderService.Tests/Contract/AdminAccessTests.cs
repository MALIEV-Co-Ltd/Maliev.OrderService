using System.Net;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class AdminAccessTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private static readonly string[] AdminRoles = ["admin"];

        [Fact]
        public async Task AdminCanDeleteOrder()
        {
            // Arrange
            // Admin role should grant delete access
            HttpClient client = _factory.CreateAuthenticatedClient("admin-user", roles: AdminRoles, permissions: [OrderPermissions.OrdersCancel]);

            string orderId = await CreateTestOrderAsync();

            // Act
            HttpResponseMessage response = await client.DeleteAsync($"/order/v1/orders/{orderId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AdminCanAccessReports()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient(
                "admin-user",
                roles: AdminRoles,
                permissions: [OrderPermissions.ReportsSales]);

            // Act
            HttpResponseMessage response = await client.GetAsync("/order/v1/reports/sales");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

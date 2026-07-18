using System.Net;
using System.Net.Http.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Domain.Entities;
using Maliev.OrderService.Infrastructure.Persistence;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class CreatorOwnershipTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private static readonly string[] ReadPermissions = [OrderPermissions.OrdersRead];
        private static readonly string[] ManagePermissions = [OrderPermissions.OrdersRead, OrderPermissions.OrdersUpdate, OrderPermissions.OrdersCancel];
        private static readonly string[] CreatorRoles = ["roles.order.creator"];

        [Fact]
        public async Task CreatorCanReadOwnOrder()
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
        public async Task CreatorCannotReadOthersOrder()
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

        [Fact]
        public async Task CreatorCannotAccessOthersOrderSubresourceRoutes()
        {
            // Arrange
            string creator1 = "creator-001";
            string creator2 = "creator-002";

            HttpClient client = _factory.CreateAuthenticatedClient(creator1,
                roles: CreatorRoles,
                permissions: ManagePermissions);

            string othersOrderId = await CreateTestOrderAsync(creator2);

            var requests = new List<(string Name, Func<Task<HttpResponseMessage>> Send)>
            {
                ("GET statuses", () => client.GetAsync($"/order/v1/orders/{othersOrderId}/statuses")),
                ("GET files", () => client.GetAsync($"/order/v1/orders/{othersOrderId}/files")),
                ("GET notes", () => client.GetAsync($"/order/v1/orders/{othersOrderId}/notes")),
                ("GET preview images", () => client.GetAsync($"/order/v1/orders/{othersOrderId}/preview-images")),
                ("GET file download", () => client.GetAsync($"/order/v1/orders/{othersOrderId}/files/1")),
                ("DELETE file", () => client.DeleteAsync($"/order/v1/orders/{othersOrderId}/files/1")),
                ("PUT set primary file", () => client.PutAsync($"/order/v1/orders/{othersOrderId}/files/1/set-primary", null)),
                ("PUT order", () => client.PutAsJsonAsync($"/order/v1/orders/{othersOrderId}", new { version = "1", requirements = "blocked" })),
                ("DELETE order", () => client.DeleteAsync($"/order/v1/orders/{othersOrderId}")),
                ("POST cancel", () => client.PostAsJsonAsync($"/order/v1/orders/{othersOrderId}/cancel", new { cancellationReason = "blocked" })),
                ("PATCH outsourcing", () => client.PatchAsJsonAsync($"/order/v1/orders/{othersOrderId}/outsourcing", new { isOutsourced = true, supplierName = "Blocked Supplier" })),
                ("POST status", () => client.PostAsJsonAsync($"/order/v1/orders/{othersOrderId}/statuses", new { status = "Reviewing" })),
                ("POST note", () => client.PostAsJsonAsync($"/order/v1/orders/{othersOrderId}/notes", new { noteType = "customer", noteText = "blocked" })),
                ("PUT batch", () => client.PutAsJsonAsync("/order/v1/orders/batch", new[] { new { orderId = othersOrderId, version = "1", requirements = "blocked" } })),
                ("POST batch cancel", () => client.PostAsJsonAsync("/order/v1/orders/batch/cancel", new[] { othersOrderId }))
            };

            foreach ((string name, Func<Task<HttpResponseMessage>> send) in requests)
            {
                // Act
                HttpResponseMessage response = await send();

                // Assert
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
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
                CreatedBy = "system",
                UpdatedBy = "system"
            };
            _ = context.Orders.Add(order);
            _ = await context.SaveChangesAsync();
            return order.OrderId;
        }
    }
}

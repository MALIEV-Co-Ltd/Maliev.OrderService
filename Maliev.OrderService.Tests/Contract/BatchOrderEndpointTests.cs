using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.OrderService.Api.Authorization;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class BatchOrderEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private readonly HttpClient _client = factory.CreateAuthenticatedClient(
                "test-admin",
                AdminRoles,
                permissions: OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task PostBatchOrdersCreatesMultipleOrders()
        {
            // Arrange
            await _factory.ResetDatabaseAsync();
            var batchRequest = new[]
            {
                new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 },
                new { customerId = "CUST-002", customerType = "Customer", serviceCategoryId = 1 }
            };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders/batch", batchRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PutBatchOrdersUpdatesMultipleOrders()
        {
            // Arrange - Create 2 orders first
            await _factory.ResetDatabaseAsync();
            var order1Request = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var order2Request = new { customerId = "CUST-002", customerType = "Customer", serviceCategoryId = 1 };

            HttpResponseMessage createResponse1 = await _client.PostAsJsonAsync("/order/v1/orders", order1Request);
            JsonElement createdOrder1 = await createResponse1.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId1 = createdOrder1.GetProperty("orderId").GetString();
            string? version1 = createdOrder1.GetProperty("version").GetString();

            HttpResponseMessage createResponse2 = await _client.PostAsJsonAsync("/order/v1/orders", order2Request);
            JsonElement createdOrder2 = await createResponse2.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId2 = createdOrder2.GetProperty("orderId").GetString();
            string? version2 = createdOrder2.GetProperty("version").GetString();

            var batchRequest = new[]
            {
                new { orderId = orderId1, version = version1, assignedEmployeeId = "EMP-001" },
                new { orderId = orderId2, version = version2, assignedEmployeeId = "EMP-002" }
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync("/order/v1/orders/batch", batchRequest);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Conflict,
                $"Expected OK or Conflict, but got {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}");
        }

        [Fact]
        public async Task DeleteBatchOrdersCancelsMultipleOrders()
        {
            // Arrange - Create 2 orders first
            await _factory.ResetDatabaseAsync();
            var order1Request = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var order2Request = new { customerId = "CUST-002", customerType = "Customer", serviceCategoryId = 1 };

            HttpResponseMessage createResponse1 = await _client.PostAsJsonAsync("/order/v1/orders", order1Request);
            JsonElement createdOrder1 = await createResponse1.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId1 = createdOrder1.GetProperty("orderId").GetString();

            HttpResponseMessage createResponse2 = await _client.PostAsJsonAsync("/order/v1/orders", order2Request);
            JsonElement createdOrder2 = await createResponse2.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId2 = createdOrder2.GetProperty("orderId").GetString();

            string?[] orderIds = [orderId1, orderId2];

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders/batch/cancel", orderIds);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostBatchOrdersValidationFailureReturnsBadRequest()
        {
            // Arrange
            var batchRequest = new[]
            {
                new { customerId = "", customerType = "Customer", serviceCategoryId = 1 } // Missing customerId
            };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders/batch", batchRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostBatchCancelNotFoundReturnsNotFound()
        {
            // Arrange
            string[] orderIds = ["NON-EXISTENT-1"];

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders/batch/cancel", orderIds);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

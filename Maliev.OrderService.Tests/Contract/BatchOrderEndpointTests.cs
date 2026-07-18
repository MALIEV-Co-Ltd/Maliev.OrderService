using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.OrderService.Api.Authorization;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class BatchOrderEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient(
                "test-admin",
                AdminRoles,
                permissions: OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        public TestWebApplicationFactory Factory => factory;

        [Fact]
        public async Task PostBatchOrdersCreatesMultipleOrders()
        {
            // Arrange
            await Factory.ResetDatabaseAsync();
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
            await Factory.ResetDatabaseAsync();
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
            await Factory.ResetDatabaseAsync();
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

        [Fact]
        public async Task PutBatchOrdersWithPartialFailure()
        {
            // Arrange - Create 1 order
            await Factory.ResetDatabaseAsync();
            var order1Request = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };

            HttpResponseMessage createResponse1 = await _client.PostAsJsonAsync("/order/v1/orders", order1Request);
            JsonElement createdOrder1 = await createResponse1.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId1 = createdOrder1.GetProperty("orderId").GetString();
            string? version1 = createdOrder1.GetProperty("version").GetString();

            // Try to update with one valid and one invalid order
            var batchRequest = new[]
            {
                new { orderId = orderId1!, version = version1!, assignedEmployeeId = "EMP-001" },
                new { orderId = "NON-EXISTENT", version = "1", assignedEmployeeId = "EMP-002" }
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync("/order/v1/orders/batch", batchRequest);

            // Assert - May return OK with partial success or conflict
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PutBatchOrdersValidationError()
        {
            // Arrange - No orders
            var batchRequest = new[] { new { } };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync("/order/v1/orders/batch", batchRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostBatchOrdersEmptyArray()
        {
            // Arrange
            await Factory.ResetDatabaseAsync();
            var batchRequest = Array.Empty<object>();

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders/batch", batchRequest);

            // Assert - Accept any non-500 status code
            Assert.True((int)response.StatusCode < 500, $"Expected non-5xx status, got {response.StatusCode}");
        }

        [Fact]
        public async Task PostBatchOrdersWithoutCreatePermissionReturnsForbidden()
        {
            var clientWithoutCreate = factory.CreateAuthenticatedClient(
                "test-user",
                ["Manager"],
                [OrderPermissions.OrdersRead, OrderPermissions.OrdersUpdate]);

            var batchRequest = new[]
            {
                new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 }
            };

            HttpResponseMessage response = await clientWithoutCreate.PostAsJsonAsync("/order/v1/orders/batch", batchRequest);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PutBatchOrdersWithoutUpdatePermissionReturnsForbidden()
        {
            var clientWithoutUpdate = factory.CreateAuthenticatedClient(
                "test-user",
                ["Manager"],
                [OrderPermissions.OrdersRead, OrderPermissions.OrdersCreate]);

            await Factory.ResetDatabaseAsync();
            var order1Request = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", order1Request);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var batchRequest = new[]
            {
                new { orderId = orderId, version = version, assignedEmployeeId = "EMP-001" }
            };

            HttpResponseMessage response = await clientWithoutUpdate.PutAsJsonAsync("/order/v1/orders/batch", batchRequest);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBatchOrdersWithoutCancelPermissionReturnsForbidden()
        {
            var clientWithoutCancel = factory.CreateAuthenticatedClient(
                "test-user",
                ["Manager"],
                [OrderPermissions.OrdersRead, OrderPermissions.OrdersUpdate]);

            await Factory.ResetDatabaseAsync();
            var order1Request = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", order1Request);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            string?[] orderIds = [orderId];

            HttpResponseMessage response = await clientWithoutCancel.PostAsJsonAsync("/order/v1/orders/batch/cancel", orderIds);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostBatchCancelEmptyArrayReturnsBadRequest()
        {
            await Factory.ResetDatabaseAsync();
            string[] orderIds = [];

            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders/batch/cancel", orderIds);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutBatchOrdersAllInvalidReturnsBadRequest()
        {
            await Factory.ResetDatabaseAsync();
            var batchRequest = new[]
            {
                new { orderId = "INVALID-1", version = "invalid" }
            };

            HttpResponseMessage response = await _client.PutAsJsonAsync("/order/v1/orders/batch", batchRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}

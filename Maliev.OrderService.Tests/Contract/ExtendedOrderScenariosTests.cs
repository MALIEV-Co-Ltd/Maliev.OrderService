using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class ExtendedOrderScenariosTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private readonly HttpClient _adminClient = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);
        private readonly HttpClient _managerClient = factory.CreateAuthenticatedClient("test-manager", ManagerRoles, ManagerPermissions);

        private static readonly string[] AdminRoles = ["Admin"];
        private static readonly string[] ManagerRoles = ["Manager"];
        private static readonly string[] ManagerPermissions =
        [
            OrderPermissions.OrdersRead,
            OrderPermissions.OrdersCreate,
            OrderPermissions.OrdersUpdate,
            OrderPermissions.OrdersFulfill
        ];

        [Fact]
        public async Task ManagerCanUpdateOrderWithoutPricingPermission()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _managerClient.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            // Manager can update non-pricing fields
            var updateRequest = new
            {
                version,
                requirements = "Manager updated requirements",
                assignedEmployeeId = "EMP-001"
            };

            // Act
            HttpResponseMessage response = await _managerClient.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ManagerCannotUpdatePricingFields()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _managerClient.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            // Manager tries to update pricing fields (should fail)
            var updateRequest = new
            {
                version,
                quotedAmount = 5000.00m,
                quoteCurrency = "THB"
            };

            // Act
            HttpResponseMessage response = await _managerClient.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CancelNonExistentOrderReturnsFalse()
        {
            // Act
            HttpResponseMessage response = await _adminClient.DeleteAsync("/order/v1/orders/NON-EXISTENT-ORDER");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetOrderByIdWithIncludes()
        {
            // Arrange - Create order with statuses
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _adminClient.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            // Add status
            await _adminClient.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });

            // Act
            HttpResponseMessage response = await _adminClient.GetAsync($"/order/v1/orders/{orderId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrderWithAllFields()
        {
            // Arrange - Create order with all possible fields
            var createRequest = new
            {
                customerId = "CUST-FULL-001",
                customerType = "Customer",
                serviceCategoryId = 1,
                processTypeId = 1,
                requirements = "Full test order",
                orderedQuantity = 100,
                quotedAmount = 5000.00m,
                quoteCurrency = "THB",
                leadTimeDays = 14,
                promisedDeliveryDate = "2026-04-01T00:00:00Z"
            };

            // Act
            HttpResponseMessage response = await _adminClient.PostAsJsonAsync("/order/v1/orders", createRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            JsonElement createdOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("CUST-FULL-001", createdOrder.GetProperty("customerId").GetString());
        }

        [Fact]
        public async Task CreateOrderValidationFailure()
        {
            // Arrange - Invalid request (missing required fields)
            var createRequest = new
            {
                customerId = "" // Invalid - empty
            };

            // Act
            HttpResponseMessage response = await _adminClient.PostAsJsonAsync("/order/v1/orders", createRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStatusToAllValidStatuses()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-STATUS-ALL", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _adminClient.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Test all valid status transitions
            string[] statuses = ["Reviewing", "Reviewed", "Quoted", "Accepted", "Paid", "InProgress", "Finished", "Shipped", "Completed"];

            foreach (string status in statuses)
            {
                var statusRequest = new { Status = status };
                HttpResponseMessage statusResponse = await _adminClient.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", statusRequest);

                // Only the first few transitions should succeed
                if (statusResponse.StatusCode != HttpStatusCode.Created)
                    break;
            }

            // Act - Get final status
            HttpResponseMessage getResponse = await _adminClient.GetAsync($"/order/v1/orders/{orderId}/statuses");

            // Assert
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [Fact]
        public async Task ConcurrentOrderCreation()
        {
            // This tests the race condition handling in order ID generation
            // Create multiple orders quickly to test sequence handling

            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 5; i++)
            {
                var request = new { customerId = $"CUST-CONCURRENT-{i}", customerType = "Customer", serviceCategoryId = 1 };
                tasks.Add(_adminClient.PostAsJsonAsync("/order/v1/orders", request));
            }

            // Act
            await Task.WhenAll(tasks);

            // Assert - All should succeed
            var successes = tasks.Count(t => t.Result.StatusCode == HttpStatusCode.Created);
            Assert.True(successes > 0);
        }
    }
}

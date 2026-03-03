using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class OrderUpdateTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task UpdateOrderWithRequirementsUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                requirements = "Updated requirements for the order"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement updatedOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Updated requirements for the order", updatedOrder.GetProperty("requirements").GetString());
        }

        [Fact]
        public async Task UpdateOrderWithQuantityUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                orderedQuantity = 50
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement updatedOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(50, updatedOrder.GetProperty("orderedQuantity").GetInt32());
        }

        [Fact]
        public async Task UpdateOrderWithManufacturedQuantityUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                manufacturedQuantity = 45
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithMaterialIdUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                materialId = 1
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithColorIdUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                colorId = 2
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithSurfaceFinishingIdUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                surfaceFinishingId = 1
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithLeadTimeDaysUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                leadTimeDays = 7
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithPromisedDeliveryDateUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                promisedDeliveryDate = "2026-03-15T00:00:00Z"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithActualDeliveryDateUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                actualDeliveryDate = "2026-03-10T00:00:00Z"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithQuotedAmountUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                quotedAmount = 1500.00m,
                quoteCurrency = "THB"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement updatedOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1500.00m, updatedOrder.GetProperty("quotedAmount").GetDecimal());
        }

        [Fact]
        public async Task UpdateOrderWithDepartmentIdUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                departmentId = "DEPT-001"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithCustomerPoNumberUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                customerPoNumber = "PO-12345"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement updatedOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("PO-12345", updatedOrder.GetProperty("customerPoNumber").GetString());
        }

        [Fact]
        public async Task UpdateOrderWithInvalidVersionReturnsBadRequest()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var updateRequest = new
            {
                version = "invalid-version",
                assignedEmployeeId = "EMP-001"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderConcurrencyConflictReturns409()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                assignedEmployeeId = "EMP-001"
            };

            // First update should succeed
            await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Second update with same version should fail (concurrency conflict)
            HttpResponseMessage conflictResponse = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithMultipleFieldsUpdatesSuccessfully()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            // Update with multiple fields at once
            var updateRequest = new
            {
                version,
                requirements = "Updated requirements",
                orderedQuantity = 100,
                quotedAmount = 5000.00m,
                quoteCurrency = "THB",
                leadTimeDays = 14
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement updatedOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Updated requirements", updatedOrder.GetProperty("requirements").GetString());
            Assert.Equal(100, updatedOrder.GetProperty("orderedQuantity").GetInt32());
        }
    }
}

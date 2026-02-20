using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class OrderEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task GET_Orders_Returns_PaginatedList()
        {
            // Arrange - This test will FAIL until GET /orders endpoint is implemented

            // Act
            HttpResponseMessage response = await _client.GetAsync("/order/v1/orders");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task POST_Orders_Creates_Order_With_Validation()
        {
            // Arrange - This test will FAIL until POST /orders endpoint is implemented
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1,
                processTypeId = 1,
                requirements = "Test order requirements"
            };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task GET_OrderById_Returns_Order()
        {
            // Arrange - First create an order
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string orderId = createdOrder.GetProperty("orderId").GetString()!;

            // Act
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains(orderId, content);
        }

        [Fact]
        public async Task GET_OrderById_NotFound_Returns404()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync("/order/v1/orders/NON-EXISTENT-ID");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Order_Updates_With_OptimisticConcurrency()
        {
            // Arrange - First create an order
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                assignedEmployeeId = "EMP-001"
            };

            // Act
            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Try updating again with OLD version (concurrency conflict)
            HttpResponseMessage conflictResponse = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);
            Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        }

        [Fact]
        public async Task PUT_Order_PricingUpdate_WithoutPermission_ReturnsForbidden()
        {
            // Arrange
            var clientWithoutApprove = factory.CreateAuthenticatedClient("test-user", ["Manager"], [OrderPermissions.OrdersUpdate, OrderPermissions.OrdersRead]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                quotedAmount = 1000m,
                quoteCurrency = "USD"
            };

            // Act
            HttpResponseMessage response = await clientWithoutApprove.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task POST_CancelWithReason_ReturnsOk()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var cancelRequest = new { cancellationReason = "Customer request" };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/cancel", cancelRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Customer request", content);
        }

        [Fact]
        public async Task GET_SubResources_ReturnOk()
        {
            // Arrange
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            // Act
            var statusesResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/statuses");
            var filesResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/files");
            var notesResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/notes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, statusesResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, filesResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);
        }


        [Fact]
        public async Task DELETE_Order_Cancels_Order()
        {
            // Arrange - First create an order
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            // Act
            HttpResponseMessage response = await _client.DeleteAsync($"/order/v1/orders/{orderId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

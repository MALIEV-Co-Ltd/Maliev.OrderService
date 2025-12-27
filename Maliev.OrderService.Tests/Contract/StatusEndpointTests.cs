using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class StatusEndpointTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task GET_OrderStatuses_Returns_History()
        {
            // Arrange - First create an order
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var orderId = createdOrder.GetProperty("orderId").GetString();

            // Act
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/statuses");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task POST_OrderStatus_Updates_Status_With_StateTransition()
        {
            // Arrange - First create an order
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var orderId = createdOrder.GetProperty("orderId").GetString();

            var statusRequest = new
            {
                Status = "Reviewing",
                InternalNotes = "Internal review notes",
                CustomerNotes = "Your order is being reviewed"
            };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", statusRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}

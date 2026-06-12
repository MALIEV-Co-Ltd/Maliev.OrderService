using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class StatusEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task GetOrderStatusesReturnsHistory()
        {
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/statuses");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task PostOrderStatusTransitionsThroughQuotedAcceptedPaid()
        {
            // 1. Create Order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // 2. New -> Reviewing
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            // 3. Reviewing -> Reviewed
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            // 4. Reviewed -> Quoted
            var quotedResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            Assert.Equal(HttpStatusCode.Created, quotedResponse.StatusCode);
            // 5. Quoted -> Accepted
            var acceptedResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            Assert.Equal(HttpStatusCode.Created, acceptedResponse.StatusCode);
            // 6. Accepted -> Paid
            var paidResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Paid" });
            Assert.Equal(HttpStatusCode.Created, paidResponse.StatusCode);
            // 7. Paid -> InProgress
            var inProgressResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            Assert.Equal(HttpStatusCode.Created, inProgressResponse.StatusCode);
            // 8. InProgress -> Finished
            var finishedResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Finished" });
            Assert.Equal(HttpStatusCode.Created, finishedResponse.StatusCode);
            // 9. Finished -> QualityReleased
            var qualityReleasedResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "QualityReleased" });
            Assert.Equal(HttpStatusCode.Created, qualityReleasedResponse.StatusCode);
            // 10. QualityReleased -> Shipped
            var shippedResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Shipped" });
            Assert.Equal(HttpStatusCode.Created, shippedResponse.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusInvalidTransitionReturnsBadRequest()
        {
            // 1. Create Order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // 2. Try New -> Shipped (Invalid)
            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Shipped" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}

using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class ExtendedStatusEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task PostOrderStatusTransitionToOnHold()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Paid" });
            var inProgressResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            Assert.Equal(HttpStatusCode.Created, inProgressResponse.StatusCode);

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "OnHold", InternalNotes = "Test hold" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusTransitionFromOnHoldBackToInProgress()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Paid" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "OnHold" });

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusTransitionToRejected()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Rejected", InternalNotes = "Not suitable" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusTransitionToDeclined()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Declined" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusTransitionToPOIssued()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            var acceptedResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            Assert.Equal(HttpStatusCode.Created, acceptedResponse.StatusCode);

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "POIssued", InternalNotes = "PO-12345" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusFromFinishedToReopen()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Paid" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Finished" });

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reopen", InternalNotes = "Customer requested changes" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusFromReopenBackToInProgress()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Paid" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Finished" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reopen" });

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusInvalidStatusReturnsBadRequest()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InvalidStatus" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusSameStatusReturnsBadRequest()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "New" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusCancelledOrderCannotTransition()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            var cancelledResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Cancelled" });
            Assert.Equal(HttpStatusCode.Created, cancelledResponse.StatusCode);

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusTransitionToShipped()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Paid" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Finished" });

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Shipped", InternalNotes = "Order shipped via Flash Express" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusTransitionToExpired()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Expired", InternalNotes = "Quote expired" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderStatusTransitionFromShippedToReopen()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Accepted" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Paid" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "InProgress" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Finished" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Shipped" });

            var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reopen", InternalNotes = "Customer reported issue" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}

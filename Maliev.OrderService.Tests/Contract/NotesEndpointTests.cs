using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class NotesEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task GetOrderNotesReturnsNotesList()
        {
            // Arrange - Create an order first
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
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/notes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderNoteCreatesNoteWithRbac()
        {
            // Arrange - Create an order first
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var noteRequest = new
            {
                NoteType = "customer",  // Fixed: proper case
                NoteText = "Customer note text"  // Fixed: proper case
            };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderNoteWithInternalTypeCreatesNote()
        {
            // Arrange - Create an order first
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var noteRequest = new
            {
                NoteType = "internal",
                NoteText = "Internal note for the team"
            };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            JsonElement noteResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("internal", noteResponse.GetProperty("noteType").GetString());
        }

        [Fact]
        public async Task PostOrderNoteValidationFailureReturnsBadRequest()
        {
            // Arrange - Create an order first
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            // Invalid note - missing NoteText
            var noteRequest = new
            {
                NoteType = "customer"
            };

            // Act
            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderNoteWithoutUpdatePermissionReturnsForbidden()
        {
            var clientWithoutUpdate = factory.CreateAuthenticatedClient("test-user", ["Customer"], [OrderPermissions.OrdersRead]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var noteRequest = new
            {
                NoteType = "customer",
                NoteText = "Customer note text"
            };

            HttpResponseMessage response = await clientWithoutUpdate.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderNoteForNonExistentOrderReturnsBadRequest()
        {
            var noteRequest = new
            {
                NoteType = "customer",
                NoteText = "Customer note text"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders/NON-EXISTENT-ORDER/notes", noteRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderNoteWithInvalidNoteTypeReturnsBadRequest()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var noteRequest = new
            {
                NoteType = "invalid_type",
                NoteText = "Some note text"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderNoteWithLongTextSucceeds()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var noteRequest = new
            {
                NoteType = "internal",
                NoteText = new string('x', 1000)
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}

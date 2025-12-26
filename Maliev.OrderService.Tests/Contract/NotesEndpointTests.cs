using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class NotesEndpointTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task GET_OrderNotes_Returns_NotesList()
        {
            // Arrange - Create an order first
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
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/notes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_OrderNote_Creates_Note_With_RBAC()
        {
            // Arrange - Create an order first
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var orderId = createdOrder.GetProperty("orderId").GetString();

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
    }
}

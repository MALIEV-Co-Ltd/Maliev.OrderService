using System.Net;
using System.Net.Http.Json;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class NotesEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly string[] AdminRoles = { "Admin" };

    public NotesEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles);
    }

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

        var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Act
        var response = await _client.GetAsync($"/order/v1/orders/{orderId}/notes");

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

        var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        var noteRequest = new
        {
            NoteType = "customer",  // Fixed: proper case
            NoteText = "Customer note text"  // Fixed: proper case
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

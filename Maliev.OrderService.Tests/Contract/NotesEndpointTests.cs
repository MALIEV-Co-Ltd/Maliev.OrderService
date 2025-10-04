using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class NotesEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NotesEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Act
        var response = await _client.GetAsync($"/orders/v1/orders/{orderId}/notes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        var noteRequest = new
        {
            NoteType = "customer",  // Fixed: proper case
            NoteText = "Customer note text"  // Fixed: proper case
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/orders/v1/orders/{orderId}/notes", noteRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

using System.Net;
using System.Net.Http.Json;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class StatusEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly string[] AdminRoles = { "Admin" };

    public StatusEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles);
    }

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

        var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Act
        var response = await _client.GetAsync($"/order/v1/orders/{orderId}/statuses");

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

        var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        var statusRequest = new
        {
            Status = "Reviewing",
            InternalNotes = "Internal review notes",
            CustomerNotes = "Your order is being reviewed"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", statusRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

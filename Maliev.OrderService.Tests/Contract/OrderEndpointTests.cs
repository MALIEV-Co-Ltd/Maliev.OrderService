using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class OrderEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly string[] AdminRoles = { "Admin" };

    public OrderEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);
    }

    [Fact]
    public async Task GET_Orders_Returns_PaginatedList()
    {
        // Arrange - This test will FAIL until GET /orders endpoint is implemented

        // Act
        var response = await _client.GetAsync("/order/v1/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
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
        var response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
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

        var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString()!;

        // Act
        var response = await _client.GetAsync($"/order/v1/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(orderId, content);
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

        var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();
        var version = createdOrder.GetProperty("version").GetString();

        var updateRequest = new
        {
            version = version,
            assignedEmployeeId = "EMP-001"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

        var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Act
        var response = await _client.DeleteAsync($"/order/v1/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

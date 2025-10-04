using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class OrderEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Orders_Returns_PaginatedList()
    {
        // Arrange - This test will FAIL until GET /orders endpoint is implemented

        // Act
        var response = await _client.GetAsync("/orders/v1/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
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
        var response = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
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

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Act
        var response = await _client.GetAsync($"/orders/v1/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(orderId);
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

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();
        var version = createdOrder.GetProperty("version").GetString();

        var updateRequest = new
        {
            version = version,
            assignedEmployeeId = "EMP-001"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/orders/v1/orders/{orderId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Act
        var response = await _client.DeleteAsync($"/orders/v1/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

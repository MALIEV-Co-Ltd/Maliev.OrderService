using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class BatchOrderEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BatchOrderEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_BatchOrders_Creates_Multiple_Orders()
    {
        // Arrange
        var batchRequest = new[]
        {
            new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 },
            new { customerId = "CUST-002", customerType = "Customer", serviceCategoryId = 1 }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/orders/v1/orders/batch", batchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PUT_BatchOrders_Updates_Multiple_Orders()
    {
        // Arrange - Create 2 orders first
        var order1Request = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
        var order2Request = new { customerId = "CUST-002", customerType = "Customer", serviceCategoryId = 1 };

        var createResponse1 = await _client.PostAsJsonAsync("/orders/v1/orders", order1Request);
        var createdOrder1 = await createResponse1.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId1 = createdOrder1.GetProperty("orderId").GetString();
        var version1 = createdOrder1.GetProperty("version").GetString();

        var createResponse2 = await _client.PostAsJsonAsync("/orders/v1/orders", order2Request);
        var createdOrder2 = await createResponse2.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId2 = createdOrder2.GetProperty("orderId").GetString();
        var version2 = createdOrder2.GetProperty("version").GetString();

        var batchRequest = new[]
        {
            new { orderId = orderId1, version = version1, assignedEmployeeId = "EMP-001" },
            new { orderId = orderId2, version = version2, assignedEmployeeId = "EMP-002" }
        };

        // Act
        var response = await _client.PutAsJsonAsync("/orders/v1/orders/batch", batchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DELETE_BatchOrders_Cancels_Multiple_Orders()
    {
        // Arrange - Create 2 orders first
        var order1Request = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
        var order2Request = new { customerId = "CUST-002", customerType = "Customer", serviceCategoryId = 1 };

        var createResponse1 = await _client.PostAsJsonAsync("/orders/v1/orders", order1Request);
        var createdOrder1 = await createResponse1.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId1 = createdOrder1.GetProperty("orderId").GetString();

        var createResponse2 = await _client.PostAsJsonAsync("/orders/v1/orders", order2Request);
        var createdOrder2 = await createResponse2.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId2 = createdOrder2.GetProperty("orderId").GetString();

        var orderIds = new[] { orderId1, orderId2 };

        // Act
        var response = await _client.PostAsJsonAsync("/orders/v1/orders/batch/cancel", orderIds);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

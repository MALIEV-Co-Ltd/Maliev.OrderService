using System.Net;
using System.Net.Http.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Tests.Testing;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class ManagerAccessTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ManagerAccessTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Manager_CanCreateOrder()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("manager-user", 
            permissions: new[] { OrderPermissions.OrdersCreate });
        
        var request = new
        {
            customerId = "CUST-001",
            customerType = "Customer",
            serviceCategoryId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/order/v1/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotDeleteOrder()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("manager-user", 
            permissions: new[] { OrderPermissions.OrdersRead }); // No delete permission
        
        var orderId = await CreateTestOrderAsync();

        // Act
        var response = await client.DeleteAsync($"/order/v1/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> CreateTestOrderAsync()
    {
        using var context = _factory.CreateDbContext();
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            ProcessTypeId = 1,
            CreatedAt = DateTime.UtcNow,
            Version = Guid.NewGuid().ToByteArray(),
            CreatedBy = "system",
            UpdatedBy = "system"
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order.OrderId;
    }
}

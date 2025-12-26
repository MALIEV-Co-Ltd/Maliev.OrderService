using System.Net;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Tests.Testing;
using System.Security.Claims;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class CreatorOwnershipTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private static readonly string[] ReadPermissions = { OrderPermissions.OrdersRead };
    private static readonly string[] CreatorRoles = { "roles.order.creator" };

    public CreatorOwnershipTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Creator_CanReadOwnOrder()
    {
        // Arrange
        var userId = "creator-001";
        var client = _factory.CreateAuthenticatedClient(userId, 
            roles: CreatorRoles,
            permissions: ReadPermissions);
        
        var orderId = await CreateTestOrderAsync(userId);

        // Act
        var response = await client.GetAsync($"/order/v1/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Creator_CannotReadOthersOrder()
    {
        // Arrange
        var creator1 = "creator-001";
        var creator2 = "creator-002";
        
        var client = _factory.CreateAuthenticatedClient(creator1, 
            roles: CreatorRoles,
            permissions: ReadPermissions);
        
        var othersOrderId = await CreateTestOrderAsync(creator2);

        // Act
        var response = await client.GetAsync($"/order/v1/orders/{othersOrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> CreateTestOrderAsync(string customerId)
    {
        using var context = _factory.CreateDbContext();
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = customerId,
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

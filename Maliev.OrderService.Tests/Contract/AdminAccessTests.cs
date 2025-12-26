using System.Net;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Tests.Testing;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class AdminAccessTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private static readonly string[] AdminRoles = { "admin" };

    public AdminAccessTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_CanDeleteOrder()
    {
        // Arrange
        // Admin role should grant delete access
        var client = _factory.CreateAuthenticatedClient("admin-user", roles: AdminRoles, permissions: new[] { OrderPermissions.OrdersCancel });

        var orderId = await CreateTestOrderAsync();

        // Act
        var response = await client.DeleteAsync($"/order/v1/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanAccessReports()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("admin-user", roles: AdminRoles);

        // Act
        var response = await client.GetAsync("/order/v1/reports/sales");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

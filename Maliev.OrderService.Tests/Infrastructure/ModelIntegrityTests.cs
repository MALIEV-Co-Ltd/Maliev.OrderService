using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.OrderService.Tests.Infrastructure;

public class ModelIntegrityTests
{
    [Fact]
    public void ModelShouldNotHavePendingChanges()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new OrderDbContext(options);
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges, "Run 'dotnet ef migrations add <Name> --project Maliev.OrderService.Data --startup-project Maliev.OrderService.Api'");
    }
}

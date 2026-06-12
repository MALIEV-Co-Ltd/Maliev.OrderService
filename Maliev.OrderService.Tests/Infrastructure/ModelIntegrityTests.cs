using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Tests.Infrastructure
{
    public class ModelIntegrityTests
    {
        [Fact]
        public void ModelShouldNotHavePendingChanges()
        {
            DbContextOptions<OrderDbContext> options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseNpgsql("Host=localhost;Database=ModelCheck")
                .Options;

            using OrderDbContext context = new(options);
            bool hasChanges = context.Database.HasPendingModelChanges();

            Assert.False(hasChanges, "Run 'dotnet ef migrations add <Name> --project Maliev.OrderService.Data --startup-project Maliev.OrderService.Api'");
        }

        [Fact]
        public void ModelShouldIncludeMassTransitOutboxEntities()
        {
            DbContextOptions<OrderDbContext> options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseNpgsql("Host=localhost;Database=ModelCheck")
                .Options;

            using OrderDbContext context = new(options);
            var entityNames = context.Model.GetEntityTypes()
                .Select(entity => entity.ClrType.FullName)
                .ToHashSet(StringComparer.Ordinal);

            Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.InboxState", entityNames);
            Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.OutboxMessage", entityNames);
            Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.OutboxState", entityNames);
        }
    }
}

using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Uploads;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.OrderService.Tests.Integration
{
    [Collection("Database")]
    public class ConsumerIntegrationTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;

        [Fact]
        public async Task FileDeletedEventMarkOrderFileAsDeleted()
        {
            // 1. Arrange - Setup database with an order file
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var order = new Order
                {
                    OrderId = "ORD-NOTIF-1",
                    CustomerId = "C1",
                    CustomerType = "C",
                    ServiceCategoryId = 1,
                    CreatedBy = "test",
                    UpdatedBy = "test"
                };
                context.Orders.Add(order);
                context.OrderFiles.Add(new OrderFile
                {
                    OrderId = "ORD-NOTIF-1",
                    FileName = "test.txt",
                    FileRole = "Input",
                    FileCategory = "Other",
                    FileType = "text/plain",
                    ObjectPath = "storage/path/1",
                    FileSize = 100,
                    AccessLevel = "Internal",
                    UploadedBy = "test",
                    UploadedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // 2. Act - Publish event
            var harness = _factory.Services.GetRequiredService<ITestHarness>();
            await harness.Bus.Publish(new FileDeletedEvent
            {
                Payload = new FileDeletedEventPayload
                {
                    FileId = Guid.NewGuid().ToString(),
                    StoragePath = "storage/path/1",
                    ServiceId = "order-service",
                    DeletedAt = DateTimeOffset.UtcNow
                }
            });

            // 3. Assert - Wait for consumer and check DB
            var consumerHarness = harness.GetConsumerHarness<Maliev.OrderService.Api.Consumers.FileDeletedEventConsumer>();
            Assert.True(
                await consumerHarness.Consumed.Any<FileDeletedEvent>(),
                "FileDeletedEvent should be consumed.");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var file = await context.OrderFiles.FirstOrDefaultAsync(f => f.ObjectPath == "storage/path/1");
                Assert.NotNull(file?.DeletedAt);
                Assert.Equal(DateTimeKind.Utc, file!.DeletedAt!.Value.Kind);
            }
        }
    }
}

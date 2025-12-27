using Maliev.OrderService.Api.Services.External;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Maliev.OrderService.Tests
{
    public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, OrderDbContext>
    {
        private bool _seeded;

        protected override void ConfigureEnvironmentVariables()
        {
            base.ConfigureEnvironmentVariables();

            // Set dummy URL for UploadService to prevent constructor injection failures
            Environment.SetEnvironmentVariable("UploadService__BaseUrl", "http://localhost:5003");
        }

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            base.ConfigureAdditionalServices(services);

            // Remove existing UploadServiceClient registration
            ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUploadServiceClient));
            if (descriptor != null)
            {
                _ = services.Remove(descriptor);
            }

            // Mock UploadServiceClient
            var mockUploadService = new Mock<IUploadServiceClient>();

            _ = mockUploadService.Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string objectPath, Stream fileStream, string contentType, CancellationToken ct) => new UploadFileResult
                {
                    ObjectPath = objectPath,
                    FileSizeBytes = fileStream.Length,
                    ContentType = contentType,
                    UploadedAt = DateTime.UtcNow
                });

            _ = mockUploadService.Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _ = mockUploadService.Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream([1, 2, 3]));

            _ = services.AddScoped(_ => mockUploadService.Object);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            IHost host = base.CreateHost(builder);

            if (!_seeded)
            {
                SeedReferenceData();
                _seeded = true;
            }

            return host;
        }

        private void SeedReferenceData()
        {
            using OrderDbContext context = CreateDbContext();

            // Seed service categories if they don't exist
            if (!context.ServiceCategories.Any(sc => sc.CategoryId == 1))
            {
                context.ServiceCategories.AddRange(
                    new ServiceCategory { CategoryId = 1, Name = "3D Printing", Description = "3D Printing services", IsActive = true },
                    new ServiceCategory { CategoryId = 2, Name = "CNC Machining", Description = "CNC services", IsActive = true }
                );
            }

            // Seed process types if they don't exist
            if (!context.ProcessTypes.Any(pt => pt.ProcessTypeId == 1))
            {
                context.ProcessTypes.AddRange(
                    new ProcessType { ProcessTypeId = 1, Name = "FDM", ServiceCategoryId = 1, Description = "Fused Deposition Modeling", IsActive = true },
                    new ProcessType { ProcessTypeId = 2, Name = "SLA", ServiceCategoryId = 1, Description = "Stereolithography", IsActive = true },
                    new ProcessType { ProcessTypeId = 7, Name = "Laser Cutting", ServiceCategoryId = 2, Description = "Laser cutting process", IsActive = true }
                );
            }

            _ = context.SaveChanges();
        }
    }
}

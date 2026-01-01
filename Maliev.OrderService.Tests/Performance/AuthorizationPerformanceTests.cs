using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Text.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Maliev.OrderService.Tests.Performance
{
    public class AuthorizationPerformanceTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        [Fact]
        public async Task AuthorizationCheck_Latency_ShouldBeBelow50ms()
        {
            // Arrange
            string userId = "test-user-perf";
            string permission = OrderPermissions.OrdersRead;
            var permissions = new List<string> { permission };
            string cachedData = JsonSerializer.Serialize(permissions);

            var mockIamClient = new Mock<IIamServiceClient>();
            var mockCache = new Mock<IDistributedCache>();
            var mockMeterFactory = new Mock<IMeterFactory>();
            using var meter = new Meter("TestMeter");
            var mockConfiguration = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<PermissionAuthorizationHandler>>();

            _ = mockMeterFactory.Setup(m => m.Create(It.IsAny<MeterOptions>())).Returns(meter);

            // Setup cache hit
            _ = mockCache.Setup(c => c.GetAsync($"user_permissions:{userId}", default))
                .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(cachedData));

            var handler = new PermissionAuthorizationHandler(
                mockIamClient.Object,
                mockCache.Object,
                mockMeterFactory.Object,
                mockConfiguration.Object,
                mockLogger.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId)
            ], "TestAuth"));

            var requirement = new PermissionRequirement(permission);
            var context = new AuthorizationHandlerContext([requirement], user, null);

            // Warm up
            await handler.HandleAsync(context);

            // Act
            int iterations = 1000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var testContext = new AuthorizationHandlerContext([requirement], user, null);
                await handler.HandleAsync(testContext);
            }
            sw.Stop();

            double averageLatency = sw.Elapsed.TotalMilliseconds / iterations;
            _output.WriteLine($"Average Authorization Latency (Warm Cache): {averageLatency:F4} ms");

            // Assert
            Assert.True(averageLatency < 50, $"Average latency {averageLatency}ms exceeded 50ms limit");
        }
    }
}

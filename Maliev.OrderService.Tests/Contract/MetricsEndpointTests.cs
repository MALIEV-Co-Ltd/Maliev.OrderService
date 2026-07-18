using System.Net;
using Maliev.OrderService.Api.Authorization;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class MetricsEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;

        [Fact]
        public async Task GetActiveCountWithAnalyticsPermissionReturnsOk()
        {
            var client = _factory.CreateAuthenticatedClient(
                "metrics-user",
                permissions: [OrderPermissions.ReportsAnalytics]);

            var response = await client.GetAsync("/order/v1/metrics/active-count");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetActiveCountWithoutPermissionReturnsForbidden()
        {
            var client = _factory.CreateAuthenticatedClient(
                "normal-user",
                permissions: []);

            var response = await client.GetAsync("/order/v1/metrics/active-count");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

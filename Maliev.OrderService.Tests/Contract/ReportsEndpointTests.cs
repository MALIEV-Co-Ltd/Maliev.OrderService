using System.Net;
using System.Net.Http.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.Controllers;
using Xunit;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class ReportsEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private static readonly string[] AdminRoles = ["admin"];

        [Fact]
        public async Task GetAnalyticsReportAsAdminReturnsOk()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient(
                "admin-user",
                roles: AdminRoles,
                permissions: [OrderPermissions.ReportsAnalytics]);

            // Act
            HttpResponseMessage response = await client.GetAsync("/order/v1/reports/analytics");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ExportReportAsAdminReturnsFile()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient(
                "admin-user",
                roles: AdminRoles,
                permissions: [OrderPermissions.ReportsExport]);
            var request = new ExportReportRequest { Format = "CSV" };

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/order/v1/reports/sales/export", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetSalesReportWithoutAdminReturnsForbidden()
        {
            // Arrange
            HttpClient client = _factory.CreateAuthenticatedClient("normal-user", roles: ["customer"]);

            // Act
            HttpResponseMessage response = await client.GetAsync("/order/v1/reports/sales");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

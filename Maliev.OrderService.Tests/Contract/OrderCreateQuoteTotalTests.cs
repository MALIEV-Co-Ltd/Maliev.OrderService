using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.OrderService.Api.Authorization;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public sealed class OrderCreateQuoteTotalTests(TestWebApplicationFactory factory)
    {
        private static readonly string[] _adminRoles = ["Admin"];

        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", _adminRoles, OrderPermissions.All);

        [Fact]
        public async Task PostOrdersCreatesOrderWithQuotedTotal()
        {
            var createRequest = new
            {
                customerId = "CUST-QUOTE-TOTAL-001",
                customerType = "Customer",
                serviceCategoryId = 1,
                processTypeId = 1,
                quotedAmount = 27425.75m,
                quoteCurrency = "THB",
                requirements = "Configured self-service quote total."
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            JsonElement createdOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(27425.75m, createdOrder.GetProperty("quotedAmount").GetDecimal());
            Assert.Equal("THB", createdOrder.GetProperty("quoteCurrency").GetString());
        }
    }
}

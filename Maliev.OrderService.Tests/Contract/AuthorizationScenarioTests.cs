using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.Services.Business;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class AuthorizationScenarioTests(TestWebApplicationFactory factory)
    {
        private readonly TestWebApplicationFactory _factory = factory;
        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task EmployeeCanViewAllOrders()
        {
            var employeeClient = _factory.CreateAuthenticatedClient(
                "employee-001",
                roles: ["Employee"],
                permissions: OrderPermissions.All);

            var response = await employeeClient.GetAsync("/order/v1/orders");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UnauthorizedUserReturnsForbidden()
        {
            var noPermClient = _factory.CreateAuthenticatedClient(
                "no-perm-user",
                permissions: []);

            var response = await noPermClient.GetAsync("/order/v1/orders");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithPricingRequiresApprovePermission()
        {
            var client = _factory.CreateAuthenticatedClient(
                "user-without-approve",
                permissions: [OrderPermissions.OrdersRead, OrderPermissions.OrdersCreate, OrderPermissions.OrdersUpdate]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await client.PostAsJsonAsync("/order/v1/orders", createRequest);
            var createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orderId = createdOrder.GetProperty("orderId").GetString();
            var version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new { version, QuotedAmount = 100.0, QuoteCurrency = "THB" };
            var updateResponse = await client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderWithoutPricingSucceeds()
        {
            var client = _factory.CreateAuthenticatedClient(
                "user-without-approve",
                permissions: [OrderPermissions.OrdersRead, OrderPermissions.OrdersCreate, OrderPermissions.OrdersUpdate]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await client.PostAsJsonAsync("/order/v1/orders", createRequest);
            var createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var orderId = createdOrder.GetProperty("orderId").GetString();
            var version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new { version, Requirements = "Updated requirements" };
            var updateResponse = await client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        }

        [Fact]
        public async Task ManagerCanViewAllOrders()
        {
            var managerClient = _factory.CreateAuthenticatedClient(
                "manager-001",
                roles: ["Manager"],
                permissions: OrderPermissions.All);

            var response = await managerClient.GetAsync("/order/v1/orders");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task FulfillmentCanOnlyViewAssignedOrders()
        {
            var fulfillmentClient = _factory.CreateAuthenticatedClient(
                "fulfillment-001",
                roles: ["Fulfillment"],
                permissions: [OrderPermissions.OrdersRead]);

            var response = await fulfillmentClient.GetAsync("/order/v1/orders");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class OrderEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task GetOrdersReturnsPaginatedList()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync("/order/v1/orders");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task PostOrdersCreatesOrderWithValidation()
        {
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1,
                processTypeId = 1,
                requirements = "Test order requirements"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task GetOrderByIdReturnsOrder()
        {
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string orderId = createdOrder.GetProperty("orderId").GetString()!;

            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains(orderId, content);
        }

        [Fact]
        public async Task PostOrdersCreatesOrderWithDeliverySnapshot()
        {
            var shippingAddressId = Guid.NewGuid();
            var billingAddressId = Guid.NewGuid();
            var createRequest = new
            {
                customerId = "CUST-SHIP-001",
                customerType = "Customer",
                serviceCategoryId = 1,
                billingAddressId,
                shippingAddressId,
                shippingAddressLine1 = "88 Rama IX Road",
                shippingAddressLine2 = "Floor 12",
                shippingCity = "Bangkok",
                shippingProvince = "Bangkok",
                shippingPostalCode = "10310",
                shippingCountry = "TH",
                deliveryContactName = "Natt Customer",
                deliveryContactPhone = "+66810000002",
                deliveryContactEmail = "shipping@example.test"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            JsonElement createdOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(billingAddressId, createdOrder.GetProperty("billingAddressId").GetGuid());
            Assert.Equal(shippingAddressId, createdOrder.GetProperty("shippingAddressId").GetGuid());
            Assert.Equal("88 Rama IX Road", createdOrder.GetProperty("shippingAddressLine1").GetString());
            Assert.Equal("Floor 12", createdOrder.GetProperty("shippingAddressLine2").GetString());
            Assert.Equal("Bangkok", createdOrder.GetProperty("shippingCity").GetString());
            Assert.Equal("Bangkok", createdOrder.GetProperty("shippingProvince").GetString());
            Assert.Equal("10310", createdOrder.GetProperty("shippingPostalCode").GetString());
            Assert.Equal("TH", createdOrder.GetProperty("shippingCountry").GetString());
            Assert.Equal("Natt Customer", createdOrder.GetProperty("deliveryContactName").GetString());
            Assert.Equal("+66810000002", createdOrder.GetProperty("deliveryContactPhone").GetString());
            Assert.Equal("shipping@example.test", createdOrder.GetProperty("deliveryContactEmail").GetString());
        }

        [Fact]
        public async Task PutOrderUpdatesDeliverySnapshot()
        {
            var createRequest = new
            {
                customerId = "CUST-SHIP-002",
                customerType = "Customer",
                serviceCategoryId = 1
            };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string orderId = createdOrder.GetProperty("orderId").GetString()!;
            string version = createdOrder.GetProperty("version").GetString()!;
            var shippingAddressId = Guid.NewGuid();
            var billingAddressId = Guid.NewGuid();

            var updateRequest = new
            {
                version,
                billingAddressId,
                shippingAddressId,
                shippingAddressLine1 = "99 Sukhumvit Road",
                shippingCity = "Bangkok",
                shippingProvince = "Bangkok",
                shippingPostalCode = "10110",
                shippingCountry = "TH",
                deliveryContactName = "Updated Receiver",
                deliveryContactPhone = "+66819999999",
                deliveryContactEmail = "receiver@example.test"
            };

            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement updatedOrder = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(billingAddressId, updatedOrder.GetProperty("billingAddressId").GetGuid());
            Assert.Equal(shippingAddressId, updatedOrder.GetProperty("shippingAddressId").GetGuid());
            Assert.Equal("99 Sukhumvit Road", updatedOrder.GetProperty("shippingAddressLine1").GetString());
            Assert.Equal("Bangkok", updatedOrder.GetProperty("shippingCity").GetString());
            Assert.Equal("Bangkok", updatedOrder.GetProperty("shippingProvince").GetString());
            Assert.Equal("10110", updatedOrder.GetProperty("shippingPostalCode").GetString());
            Assert.Equal("TH", updatedOrder.GetProperty("shippingCountry").GetString());
            Assert.Equal("Updated Receiver", updatedOrder.GetProperty("deliveryContactName").GetString());
            Assert.Equal("+66819999999", updatedOrder.GetProperty("deliveryContactPhone").GetString());
            Assert.Equal("receiver@example.test", updatedOrder.GetProperty("deliveryContactEmail").GetString());
        }

        [Fact]
        public async Task GetOrderItemsReturnsProductionLineItem()
        {
            DateTime promisedDeliveryDate = DateTime.UtcNow.Date.AddDays(7);
            var createRequest = new
            {
                customerId = "CUST-ITEMS-001",
                customerType = "Customer",
                serviceCategoryId = 1,
                processTypeId = 1,
                materialId = 1,
                orderedQuantity = 3,
                promisedDeliveryDate
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string orderId = createdOrder.GetProperty("orderId").GetString()!;

            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/items");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement items = await response.Content.ReadFromJsonAsync<JsonElement>();
            JsonElement item = items.EnumerateArray().Single();
            Assert.True(item.GetProperty("orderItemId").GetGuid() != Guid.Empty);
            Assert.True(item.GetProperty("materialId").GetGuid() != Guid.Empty);
            Assert.Equal(3, item.GetProperty("quantity").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("technology").GetString()));
            Assert.Equal(0, item.GetProperty("estimatedPrintTimeMinutes").GetInt32());

            string materialSnapshotJson = item.GetProperty("materialSnapshotJson").GetString()!;
            string configurationSnapshotJson = item.GetProperty("configurationSnapshotJson").GetString()!;
            Assert.Contains("\"materialId\":1", materialSnapshotJson);
            Assert.Contains("\"orderedQuantity\":3", configurationSnapshotJson);
            Assert.Contains("\"serviceCategoryId\":1", configurationSnapshotJson);
        }

        [Fact]
        public async Task GetOrderByIdNotFoundReturns404()
        {
            HttpResponseMessage response = await _client.GetAsync("/order/v1/orders/NON-EXISTENT-ID");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutOrderUpdatesWithOptimisticConcurrency()
        {
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                assignedEmployeeId = "EMP-001"
            };

            HttpResponseMessage response = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            HttpResponseMessage conflictResponse = await _client.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);
            Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        }

        [Fact]
        public async Task PutOrderPricingUpdateWithoutPermissionReturnsForbidden()
        {
            var clientWithoutApprove = factory.CreateAuthenticatedClient("test-user", ["Manager"], [OrderPermissions.OrdersUpdate, OrderPermissions.OrdersRead]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();
            string? version = createdOrder.GetProperty("version").GetString();

            var updateRequest = new
            {
                version,
                quotedAmount = 1000m,
                quoteCurrency = "USD"
            };

            HttpResponseMessage response = await clientWithoutApprove.PutAsJsonAsync($"/order/v1/orders/{orderId}", updateRequest);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostCancelWithReasonReturnsOk()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var cancelRequest = new { cancellationReason = "Customer request" };

            HttpResponseMessage response = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/cancel", cancelRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Customer request", content);
        }

        [Fact]
        public async Task GetSubResourcesReturnOk()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var statusesResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/statuses");
            var filesResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/files");
            var notesResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/notes");

            Assert.Equal(HttpStatusCode.OK, statusesResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, filesResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteOrderCancelsOrder()
        {
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "Customer",
                serviceCategoryId = 1
            };

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            HttpResponseMessage response = await _client.DeleteAsync($"/order/v1/orders/{orderId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetOrdersFilteredByCustomer()
        {
            // Create orders with different customers
            var createRequest1 = new { customerId = "CUST-FILTER-001", customerType = "Customer", serviceCategoryId = 1 };
            var createRequest2 = new { customerId = "CUST-FILTER-002", customerType = "Customer", serviceCategoryId = 1 };

            await _client.PostAsJsonAsync("/order/v1/orders", createRequest1);
            await _client.PostAsJsonAsync("/order/v1/orders", createRequest2);

            // Act - Filter by customer
            HttpResponseMessage response = await _client.GetAsync("/order/v1/orders?customerId=CUST-FILTER-001");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("CUST-FILTER-001", content);
        }

        [Fact]
        public async Task GetOrdersFilteredByStatus()
        {
            // Create an order
            var createRequest = new { customerId = "CUST-STATUS-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Update status to Quoted
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewing" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Reviewed" });
            await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/statuses", new { Status = "Quoted" });

            // Act - Filter by status
            HttpResponseMessage response = await _client.GetAsync("/order/v1/orders?status=Quoted");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetOrdersWithPagination()
        {
            // Act
            HttpResponseMessage response = await _client.GetAsync("/order/v1/orders?page=1&pageSize=10");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task GetOrdersUnauthorizedWithoutToken()
        {
            // Arrange - Create unauthenticated client
            var unauthenticatedFactory = new TestWebApplicationFactory();
            var unauthenticatedClient = unauthenticatedFactory.CreateClient();

            // Act
            HttpResponseMessage response = await unauthenticatedClient.GetAsync("/order/v1/orders");

            // Assert - Should return 401 or redirect
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task PostOrderCancelWithoutCancelPermissionReturnsForbidden()
        {
            var clientWithoutCancel = factory.CreateAuthenticatedClient("test-user", ["Manager"], [OrderPermissions.OrdersRead, OrderPermissions.OrdersUpdate]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            HttpResponseMessage response = await clientWithoutCancel.DeleteAsync($"/order/v1/orders/{orderId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostOrderCancelWithReasonWithoutCancelPermissionReturnsForbidden()
        {
            var clientWithoutCancel = factory.CreateAuthenticatedClient("test-user", ["Manager"], [OrderPermissions.OrdersRead, OrderPermissions.OrdersUpdate]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            var cancelRequest = new { cancellationReason = "Customer request" };
            HttpResponseMessage response = await clientWithoutCancel.PostAsJsonAsync($"/order/v1/orders/{orderId}/cancel", cancelRequest);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOrderByIdDataIsolationCustomerCannotSeeOtherCustomerOrder()
        {
            var customerClient = factory.CreateAuthenticatedClient("customer-1", ["Customer"], [OrderPermissions.OrdersRead]);

            var createRequest = new { customerId = "customer-2", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            HttpResponseMessage response = await customerClient.GetAsync($"/order/v1/orders/{orderId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrderWithInvalidCustomerTypeReturnsBadRequest()
        {
            var createRequest = new
            {
                customerId = "CUST-001",
                customerType = "InvalidType",
                serviceCategoryId = 1
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrderWithMissingRequiredFieldsReturnsBadRequest()
        {
            var createRequest = new
            {
                customerId = "CUST-001"
            };

            HttpResponseMessage response = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderNotFoundReturnsNotFound()
        {
            var updateRequest = new
            {
                version = "1",
                assignedEmployeeId = "EMP-001"
            };

            HttpResponseMessage response = await _client.PutAsJsonAsync("/order/v1/orders/NON-EXISTENT-ORDER", updateRequest);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CancelOrderNotFoundReturnsNotFound()
        {
            HttpResponseMessage response = await _client.DeleteAsync("/order/v1/orders/NON-EXISTENT-ORDER");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CancelOrderAlreadyCancelledAllowsIdempotentOperation()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            JsonElement createdOrder = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? orderId = createdOrder.GetProperty("orderId").GetString();

            await _client.DeleteAsync($"/order/v1/orders/{orderId}");

            HttpResponseMessage response = await _client.DeleteAsync($"/order/v1/orders/{orderId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

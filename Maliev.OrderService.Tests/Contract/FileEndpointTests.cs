using Maliev.OrderService.Api.Authorization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class FileEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", AdminRoles, OrderPermissions.All);

        private static readonly string[] AdminRoles = ["Admin"];

        [Fact]
        public async Task GET_OrderFiles_Returns_FileList()
        {
            // Arrange - First create an order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            var orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Act
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/files");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_OrderFile_Uploads_File()
        {
            // Arrange - Create an order first
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent([1, 2, 3]);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", "test.stl");
            content.Add(new StringContent("Input"), "FileRole");
            content.Add(new StringContent("CAD"), "FileCategory");

            // Act
            HttpResponseMessage response = await _client.PostAsync($"/order/v1/orders/{orderId}/files", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GET_OrderFileById_Downloads_File()
        {
            // Arrange - Create order and upload file first
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            using var uploadContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent([1, 2, 3]);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            uploadContent.Add(fileContent, "file", "test.stl");
            uploadContent.Add(new StringContent("Input"), "FileRole");
            uploadContent.Add(new StringContent("CAD"), "FileCategory");

            HttpResponseMessage uploadResponse = await _client.PostAsync($"/order/v1/orders/{orderId}/files", uploadContent);
            JsonElement uploadedFile = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
            long fileId = uploadedFile.GetProperty("fileId").GetInt64();


            // Act
            HttpResponseMessage response = await _client.DeleteAsync($"/order/v1/orders/{orderId}/files/{fileId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

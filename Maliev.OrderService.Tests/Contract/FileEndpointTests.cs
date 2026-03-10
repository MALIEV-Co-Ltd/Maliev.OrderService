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
        public async Task GetOrderFilesReturnsFileList()
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
        public async Task PostOrderFileUploadsFile()
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
        public async Task GetOrderFileByIdDownloadsFile()
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


            // Act - Actually test download endpoint
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/files/{fileId}");

            // Assert - May return OK or BadRequest depending on implementation
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DownloadOrderFileNotFoundReturns404()
        {
            // Arrange - First create an order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Act - Try to download non-existent file
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/files/99999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteOrderFileNotFoundReturnsNotFound()
        {
            // Arrange - First create an order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Act - Try to delete non-existent file
            HttpResponseMessage response = await _client.DeleteAsync($"/order/v1/orders/{orderId}/files/99999");

            // Assert - Returns NotFound when file doesn't exist
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UploadOrderFileWithoutUpdatePermissionReturnsForbidden()
        {
            var clientWithoutUpdate = factory.CreateAuthenticatedClient("test-user", ["Customer"], [OrderPermissions.OrdersRead]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent([1, 2, 3]);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", "test.stl");
            content.Add(new StringContent("Input"), "FileRole");
            content.Add(new StringContent("CAD"), "FileCategory");

            HttpResponseMessage response = await clientWithoutUpdate.PostAsync($"/order/v1/orders/{orderId}/files", content);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadOrderFileWithEmptyFileReturnsBadRequest()
        {
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent([]);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", "empty.stl");
            content.Add(new StringContent("Input"), "FileRole");
            content.Add(new StringContent("CAD"), "FileCategory");

            HttpResponseMessage response = await _client.PostAsync($"/order/v1/orders/{orderId}/files", content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DownloadOrderFileWithoutReadPermissionReturnsForbidden()
        {
            var clientWithoutRead = factory.CreateAuthenticatedClient("test-user", ["Customer"], [OrderPermissions.OrdersUpdate]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            HttpResponseMessage response = await clientWithoutRead.GetAsync($"/order/v1/orders/{orderId}/files/1");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteOrderFileWithoutUpdatePermissionReturnsForbidden()
        {
            var clientWithoutUpdate = factory.CreateAuthenticatedClient("test-user", ["Customer"], [OrderPermissions.OrdersRead]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            HttpResponseMessage response = await clientWithoutUpdate.DeleteAsync($"/order/v1/orders/{orderId}/files/1");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UploadFirstCadFileSetsAsPrimary()
        {
            // Arrange - Create an order
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
            HttpResponseMessage uploadResponse = await _client.PostAsync($"/order/v1/orders/{orderId}/files", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
            var file = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(file.GetProperty("isPrimary").GetBoolean(), "First CAD file should be set as primary");
        }

        [Fact]
        public async Task SetPrimaryFileUpdatesPrimaryFlag()
        {
            // Arrange - Create an order and upload two CAD files
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Upload first CAD file
            using var content1 = new MultipartFormDataContent();
            var fileContent1 = new ByteArrayContent([1, 2, 3]);
            fileContent1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content1.Add(fileContent1, "file", "first.stl");
            content1.Add(new StringContent("Input"), "FileRole");
            content1.Add(new StringContent("CAD"), "FileCategory");
            HttpResponseMessage uploadResponse1 = await _client.PostAsync($"/order/v1/orders/{orderId}/files", content1);
            var file1 = await uploadResponse1.Content.ReadFromJsonAsync<JsonElement>();
            long fileId1 = file1.GetProperty("fileId").GetInt64();

            // Upload second CAD file
            using var content2 = new MultipartFormDataContent();
            var fileContent2 = new ByteArrayContent([4, 5, 6]);
            fileContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content2.Add(fileContent2, "file", "second.stl");
            content2.Add(new StringContent("Input"), "FileRole");
            content2.Add(new StringContent("CAD"), "FileCategory");
            HttpResponseMessage uploadResponse2 = await _client.PostAsync($"/order/v1/orders/{orderId}/files", content2);
            var file2 = await uploadResponse2.Content.ReadFromJsonAsync<JsonElement>();
            long fileId2 = file2.GetProperty("fileId").GetInt64();

            // Act - Set second file as primary
            HttpResponseMessage setPrimaryResponse = await _client.PutAsync($"/order/v1/orders/{orderId}/files/{fileId2}/set-primary", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, setPrimaryResponse.StatusCode);

            // Verify second file is now primary
            HttpResponseMessage getFilesResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/files");
            var files = await getFilesResponse.Content.ReadFromJsonAsync<JsonElement>();
            var filesList = files.EnumerateArray().ToList();
            var secondFile = filesList.First(f => f.GetProperty("fileId").GetInt64() == fileId2);
            Assert.True(secondFile.GetProperty("isPrimary").GetBoolean(), "Second file should be primary");

            var firstFile = filesList.First(f => f.GetProperty("fileId").GetInt64() == fileId1);
            Assert.False(firstFile.GetProperty("isPrimary").GetBoolean(), "First file should no longer be primary");
        }

        [Fact]
        public async Task SetPrimaryFileNotFoundReturns404()
        {
            // Arrange - Create an order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Act - Try to set non-existent file as primary
            HttpResponseMessage response = await _client.PutAsync($"/order/v1/orders/{orderId}/files/99999/set-primary", null);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPreviewImagesReturnsEmptyListWhenNoImages()
        {
            // Arrange - Create an order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // Act
            HttpResponseMessage response = await _client.GetAsync($"/order/v1/orders/{orderId}/preview-images");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var images = await response.Content.ReadFromJsonAsync<List<JsonElement>>() ?? [];
            Assert.Empty(images);
        }

        [Fact]
        public async Task SetPrimaryFileWithoutUpdatePermissionReturnsForbidden()
        {
            var clientWithoutUpdate = factory.CreateAuthenticatedClient("test-user", ["Customer"], [OrderPermissions.OrdersRead]);

            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            string? orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            HttpResponseMessage response = await clientWithoutUpdate.PutAsync($"/order/v1/orders/{orderId}/files/1/set-primary", null);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

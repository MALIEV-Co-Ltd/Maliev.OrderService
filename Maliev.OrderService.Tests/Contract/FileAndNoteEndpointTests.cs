using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.OrderService.Api.Authorization;

namespace Maliev.OrderService.Tests.Contract
{
    [Collection("Database")]
    public class FileAndNoteEndpointTests(TestWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateAuthenticatedClient("test-admin", ["Admin"], OrderPermissions.All);

        [Fact]
        public async Task FileEndpoints_WorkCorrectly()
        {
            // 1. Create Order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            var orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // 2. Upload File using Form Data
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Input"), "FileRole");
            content.Add(new StringContent("CAD"), "FileCategory");
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "test.pdf");

            var uploadResponse = await _client.PostAsync($"/order/v1/orders/{orderId}/files", content);
            Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
            var fileId = (await uploadResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fileId").GetInt64();

            // 3. Download File
            var downloadResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/files/{fileId}");
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

            // 4. Delete File
            var deleteResponse = await _client.DeleteAsync($"/order/v1/orders/{orderId}/files/{fileId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task NoteEndpoints_WorkCorrectly()
        {
            // 1. Create Order
            var createRequest = new { customerId = "CUST-001", customerType = "Customer", serviceCategoryId = 1 };
            var createResponse = await _client.PostAsJsonAsync("/order/v1/orders", createRequest);
            var orderId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderId").GetString();

            // 2. Create Note
            var noteRequest = new { noteText = "Test note content", noteType = "internal" };
            var noteResponse = await _client.PostAsJsonAsync($"/order/v1/orders/{orderId}/notes", noteRequest);
            Assert.Equal(HttpStatusCode.Created, noteResponse.StatusCode);

            // 3. Get Notes
            var getResponse = await _client.GetAsync($"/order/v1/orders/{orderId}/notes");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var notes = await getResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
            Assert.NotEmpty(notes!);
        }
    }
}

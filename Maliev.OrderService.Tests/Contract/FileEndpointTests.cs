using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Maliev.OrderService.Tests.Contract;

[Collection("Database")]
public class FileEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FileEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_OrderFiles_Returns_FileList()
    {
        // Arrange - This test will FAIL until GET /orders/{orderId}/files endpoint is implemented

        // Act
        var response = await _client.GetAsync("/orders/v1/orders/ORD-2025-00001/files");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_OrderFile_Uploads_File()
    {
        // Arrange - Create an order first
        var createRequest = new
        {
            customerId = "CUST-001",
            customerType = "Customer",
            serviceCategoryId = 1
        };

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "test.stl");
        content.Add(new StringContent("Input"), "FileRole");  // Fixed: added required fields
        content.Add(new StringContent("CAD"), "FileCategory");

        // Act
        var response = await _client.PostAsync($"/orders/v1/orders/{orderId}/files", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GET_OrderFileById_Downloads_File()
    {
        // Arrange - Create order and upload file first
        var createRequest = new
        {
            customerId = "CUST-001",
            customerType = "Customer",
            serviceCategoryId = 1
        };

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Upload a file
        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        uploadContent.Add(fileContent, "file", "test.stl");
        uploadContent.Add(new StringContent("Input"), "FileRole");
        uploadContent.Add(new StringContent("CAD"), "FileCategory");

        var uploadResponse = await _client.PostAsync($"/orders/v1/orders/{orderId}/files", uploadContent);
        var uploadedFile = await uploadResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var fileId = uploadedFile.GetProperty("fileId").GetInt64();

        // Act
        var response = await _client.GetAsync($"/orders/v1/orders/{orderId}/files/{fileId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DELETE_OrderFile_Deletes_File()
    {
        // Arrange - Create order and upload file first
        var createRequest = new
        {
            customerId = "CUST-001",
            customerType = "Customer",
            serviceCategoryId = 1
        };

        var createResponse = await _client.PostAsJsonAsync("/orders/v1/orders", createRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var orderId = createdOrder.GetProperty("orderId").GetString();

        // Upload a file
        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        uploadContent.Add(fileContent, "file", "test.stl");
        uploadContent.Add(new StringContent("Input"), "FileRole");
        uploadContent.Add(new StringContent("CAD"), "FileCategory");

        var uploadResponse = await _client.PostAsync($"/orders/v1/orders/{orderId}/files", uploadContent);
        var uploadedFile = await uploadResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var fileId = uploadedFile.GetProperty("fileId").GetInt64();

        // Act
        var response = await _client.DeleteAsync($"/orders/v1/orders/{orderId}/files/{fileId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

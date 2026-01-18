using System.Net;
using System.Net.Http.Json;
using Maliev.OrderService.Api.Services.External;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.OrderService.Tests.Unit.Services
{
    public class ExternalClientTests
    {
        private readonly Mock<ILogger<AuthServiceClient>> _authLoggerMock = new();
        private readonly Mock<ILogger<CustomerServiceClient>> _customerLoggerMock = new();
        private readonly Mock<ILogger<PaymentServiceClient>> _paymentLoggerMock = new();
        private readonly Mock<ILogger<NotificationServiceClient>> _notifLoggerMock = new();
        private readonly Mock<ILogger<UploadServiceClient>> _uploadLoggerMock = new();

        [Fact]
        public async Task AuthServiceClient_ValidateTokenAsync_Success_ReturnsUserContext()
        {
            // Arrange
            var expectedContext = new UserContextDto
            {
                UserId = "user-1",
                UserType = "employee",
                Roles = new List<string> { "Admin" }
            };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = JsonContent.Create(expectedContext)
               });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://auth-service") };
            var client = new AuthServiceClient(httpClient, _authLoggerMock.Object);

            // Act
            var result = await client.ValidateTokenAsync("valid-token");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedContext.UserId, result.UserId);
        }

        [Fact]
        public async Task AuthServiceClient_ValidateTokenAsync_Error_ReturnsNull()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.Unauthorized
               });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://auth-service") };
            var client = new AuthServiceClient(httpClient, _authLoggerMock.Object);

            // Act
            var result = await client.ValidateTokenAsync("invalid-token");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CustomerServiceClient_GetCustomerDetailsAsync_Success_ReturnsDetails()
        {
            // Arrange
            var expectedDetails = new CustomerDetailsDto
            {
                CustomerId = "cust-1",
                Name = "Test Customer",
                Email = "test@example.com"
            };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = JsonContent.Create(expectedDetails)
               });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://customer-service") };
            var client = new CustomerServiceClient(httpClient, _customerLoggerMock.Object);

            // Act
            var result = await client.GetCustomerDetailsAsync("cust-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDetails.CustomerId, result.CustomerId);
        }

        [Fact]
        public async Task PaymentServiceClient_CalculatePartialChargeAsync_Success_ReturnsAmount()
        {
            // Arrange
            decimal expectedAmount = 50.5m;
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = JsonContent.Create(new { Amount = expectedAmount })
               });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://payment-service") };
            var client = new PaymentServiceClient(httpClient, _paymentLoggerMock.Object);

            // Act
            var result = await client.CalculatePartialChargeAsync("order-1", "Cancelled");

            // Assert
            Assert.Equal(expectedAmount, result);
        }

        [Fact]
        public async Task PaymentServiceClient_GetPaymentStatusAsync_Error_ReturnsNull()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ThrowsAsync(new HttpRequestException("Network error"));

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://payment-service") };
            var client = new PaymentServiceClient(httpClient, _paymentLoggerMock.Object);

            // Act
            var result = await client.GetPaymentStatusAsync("pay-1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task IamServiceClient_GetUserPermissionsAsync_Success_ReturnsPermissions()
        {
            // Arrange
            string userId = "user-1";
            var expectedPermissions = new List<string> { "orders.read", "orders.write" };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = JsonContent.Create(new { UserId = userId, Permissions = expectedPermissions })
               });

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://iam-service") });

            var client = new IamServiceClient(httpClientFactoryMock.Object, new Mock<ILogger<IamServiceClient>>().Object);

            // Act
            var result = await client.GetUserPermissionsAsync(userId);

            // Assert
            Assert.Equal(expectedPermissions.Count, result.Count());
        }

        [Fact]
        public async Task MaterialServiceClient_GetMaterialNameAsync_Success_ReturnsName()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = JsonContent.Create(new { MaterialId = 1, Name = "Aluminum" })
               });

            var cacheMock = new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://material-service") };
            var client = new MaterialServiceClient(httpClient, cacheMock.Object, new Mock<ILogger<MaterialServiceClient>>().Object);

            // Act
            var result = await client.GetMaterialNameAsync(1);

            // Assert
            Assert.Equal("Aluminum", result);
        }

        [Fact]
        public async Task EmployeeServiceClient_GetEmployeeDetailsAsync_Success_ReturnsDetails()
        {
            // Arrange
            var expected = new EmployeeDetailsDto { EmployeeId = "emp-1", Name = "John Doe", Email = "john@example.com" };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(expected) });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://emp-service") };
            var client = new EmployeeServiceClient(httpClient, new Mock<ILogger<EmployeeServiceClient>>().Object);

            // Act
            var result = await client.GetEmployeeDetailsAsync("emp-1");

            // Assert
            Assert.Equal(expected.Name, result?.Name);
        }

        [Fact]
        public async Task NotificationServiceClient_SendOrderNotificationAsync_Success_ReturnsTrue()
        {
            // Arrange
            _notifLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://notif-service") };
            var client = new NotificationServiceClient(httpClient, _notifLoggerMock.Object);

            // Act
            var result = await client.SendOrderNotificationAsync(new OrderNotificationRequest
            {
                OrderId = "1",
                CustomerId = "user-1",
                Message = "Test",
                NotificationType = "Info"
            });

#pragma warning disable CA1873
            // Assert
            Assert.True(result);
#pragma warning restore CA1873
        }

        [Fact]
        public async Task UploadServiceClient_UploadFileAsync_Success_ReturnsResult()
        {
            // Arrange
            var expected = new UploadFileResult { ObjectPath = "path/to/file", FileSizeBytes = 10, ContentType = "application/pdf", UploadedAt = DateTime.UtcNow };
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(expected) });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://upload-service") };
            var client = new UploadServiceClient(httpClient, _uploadLoggerMock.Object);

            // Act
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var result = await client.UploadFileAsync("path/to/file", stream, "application/pdf");

            // Assert
            Assert.Equal(expected.ObjectPath, result.ObjectPath);
        }
    }
}

using System.Security.Claims;
using System.Text.Json;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.Services.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics.Metrics;

namespace Maliev.OrderService.Tests.Unit.Authorization
{
    public class PermissionAuthorizationHandlerTests
    {
        private readonly Mock<IIamServiceClient> _iamClientMock = new();
        private readonly Mock<IDistributedCache> _cacheMock = new();
        private readonly Mock<IMeterFactory> _meterFactoryMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<ILogger<PermissionAuthorizationHandler>> _loggerMock = new();
        private readonly PermissionAuthorizationHandler _handler;

        public PermissionAuthorizationHandlerTests()
        {
            _configMock.Setup(c => c[It.IsAny<string>()]).Returns("test");

            // Setup MeterFactory mock to return a real Meter for testing
            var meter = new Meter("test-meter");
            _meterFactoryMock.Setup(m => m.Create(It.IsAny<MeterOptions>())).Returns(meter);

            _handler = new PermissionAuthorizationHandler(
                _iamClientMock.Object,
                _cacheMock.Object,
                _meterFactoryMock.Object,
                _configMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task HandleRequirementAsyncCacheHitSucceeds()
        {
            // Arrange
            string userId = "user-1";
            string permission = "orders.read";
            var permissions = new List<string> { permission };
            var requirement = new PermissionRequirement(permission);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            _cacheMock.Setup(c => c.GetAsync($"user_permissions:{userId}", default))
                .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(permissions));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsyncCacheMissFetchesFromIamSucceeds()
        {
            // Arrange
            string userId = "user-1";
            string permission = "orders.read";
            var permissions = new List<string> { permission };
            var requirement = new PermissionRequirement(permission);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            _cacheMock.Setup(c => c.GetAsync($"user_permissions:{userId}", default))
                .ReturnsAsync((byte[]?)null);
            _iamClientMock.Setup(i => i.GetUserPermissionsAsync(userId))
                .ReturnsAsync(permissions);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            _iamClientMock.Verify(i => i.GetUserPermissionsAsync(userId), Times.Once);
            _cacheMock.Verify(c => c.SetAsync(
                $"user_permissions:{userId}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task HandleRequirementAsyncMissingPermissionDoesNotSucceed()
        {
            // Arrange
            string userId = "user-1";
            string requiredPermission = "orders.delete";
            var userPermissions = new List<string> { "orders.read" };
            var requirement = new PermissionRequirement(requiredPermission);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            _cacheMock.Setup(c => c.GetAsync($"user_permissions:{userId}", default))
                .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(userPermissions));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsyncIamErrorDoesNotSucceed()
        {
            // Arrange
            string userId = "user-1";
            var requirement = new PermissionRequirement("orders.read");
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            _cacheMock.Setup(c => c.GetAsync($"user_permissions:{userId}", default))
                .ReturnsAsync((byte[]?)null);
            _iamClientMock.Setup(i => i.GetUserPermissionsAsync(userId))
                .ThrowsAsync(new InvalidOperationException("IAM service unavailable"));

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }
    }
}

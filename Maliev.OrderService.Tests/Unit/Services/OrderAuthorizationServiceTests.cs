using System.Security.Claims;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Domain.Entities;

namespace Maliev.OrderService.Tests.Unit.Services
{
    public class OrderAuthorizationServiceTests
    {
        private readonly OrderAuthorizationService _service = new();

        [Fact]
        public void CanViewOrderAdminRoleReturnsTrue()
        {
            // Arrange
            var user = CreateUserWithRoles("Admin");
            var order = new Order { CustomerId = "other-user" };

            // Act
            var result = _service.CanViewOrder(user, order);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanViewOrderManagerRoleReturnsTrue()
        {
            // Arrange
            var user = CreateUserWithRoles(OrderPredefinedRoles.Manager);
            var order = new Order { CustomerId = "other-user" };

            // Act
            var result = _service.CanViewOrder(user, order);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanViewOrderCreatorRoleOwnOrderReturnsTrue()
        {
            // Arrange
            string userId = "user-1";
            var user = CreateUserWithRoles(OrderPredefinedRoles.Creator, userId);
            var order = new Order { CustomerId = userId };

            // Act
            var result = _service.CanViewOrder(user, order);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanViewOrderCreatorRoleOthersOrderReturnsFalse()
        {
            // Arrange
            string userId = "user-1";
            var user = CreateUserWithRoles(OrderPredefinedRoles.Creator, userId);
            var order = new Order { CustomerId = "other-user" };

            // Act
            var result = _service.CanViewOrder(user, order);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanViewOrderFulfillmentRoleAssignedOrderReturnsTrue()
        {
            // Arrange
            string userId = "emp-1";
            var user = CreateUserWithRoles(OrderPredefinedRoles.Fulfillment, userId);
            var order = new Order { AssignedEmployeeId = userId };

            // Act
            var result = _service.CanViewOrder(user, order);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ApplyDataIsolationFilterCreatorFiltersByCustomerId()
        {
            // Arrange
            string userId = "user-1";
            var user = CreateUserWithRoles(OrderPredefinedRoles.Creator, userId);
            var orders = new List<Order>
            {
                new Order { OrderId = "1", CustomerId = userId },
                new Order { OrderId = "2", CustomerId = "other" }
            }.AsQueryable();

            // Act
            var result = _service.ApplyDataIsolationFilter(user, orders).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].OrderId);
        }

        [Fact]
        public void ApplyDataIsolationFilterFulfillmentFiltersByAssignedEmployeeId()
        {
            // Arrange
            string userId = "emp-1";
            var user = CreateUserWithRoles(OrderPredefinedRoles.Fulfillment, userId);
            var orders = new List<Order>
            {
                new Order { OrderId = "1", AssignedEmployeeId = userId },
                new Order { OrderId = "2", AssignedEmployeeId = "other" }
            }.AsQueryable();

            // Act
            var result = _service.ApplyDataIsolationFilter(user, orders).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].OrderId);
        }

        [Fact]
        public void ApplyDataIsolationFilterUnknownRoleReturnsEmpty()
        {
            // Arrange
            var user = CreateUserWithRoles("Viewer");
            var orders = new List<Order>
            {
                new Order { OrderId = "1" }
            }.AsQueryable();

            // Act
            var result = _service.ApplyDataIsolationFilter(user, orders).ToList();

            // Assert
            Assert.Empty(result);
        }

        private static ClaimsPrincipal CreateUserWithRoles(string role, string userId = "test-user")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }
    }
}

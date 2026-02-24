using System.Security.Claims;
using Maliev.OrderService.Api.Extensions;

namespace Maliev.OrderService.Tests.Unit.Extensions
{
    public class ClaimsPrincipalExtensionsTests
    {
        [Fact]
        public void GetUserIdReturnsNameIdentifier()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            Assert.Equal("user-123", principal.GetUserId());
        }

        [Fact]
        public void GetRolesReturnsAllRoles()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "Manager")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var roles = principal.GetRoles().ToList();

            Assert.Contains("Admin", roles);
            Assert.Contains("Manager", roles);
        }

        [Fact]
        public void HasPermissionReturnsTrueIfFound()
        {
            var claims = new[] { new Claim("permissions", "orders.read") };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            Assert.True(principal.HasPermission("orders.read"));
            Assert.False(principal.HasPermission("orders.write"));
        }
    }
}

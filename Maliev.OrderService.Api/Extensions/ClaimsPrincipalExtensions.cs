using System.Security.Claims;

namespace Maliev.OrderService.Api.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ClaimsPrincipal"/> to easily access common user claims.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the User ID from the claims (NameIdentifier or "sub").
        /// </summary>
        /// <param name="user">The claims principal.</param>
        /// <returns>The user ID, or "system" if not found.</returns>
        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? "system";
        }

        /// <summary>
        /// Gets the User Type from the claims (typically "userType").
        /// </summary>
        /// <param name="user">The claims principal.</param>
        /// <returns>The user type, or null if not found.</returns>
        public static string? GetUserType(this ClaimsPrincipal user)
        {
            return user.FindFirst("userType")?.Value;
        }

        /// <summary>
        /// Gets the list of roles from the claims.
        /// </summary>
        public static IEnumerable<string> GetRoles(this ClaimsPrincipal user)
        {
            return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }

        /// <summary>
        /// Checks if the user has a specific permission.
        /// Note: This is a synchronous check against claims. For IAM-synced permissions,
        /// the dynamic policy provider/handler is preferred.
        /// </summary>
        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            return user.FindAll("permissions").Any(c => string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
        }
    }
}

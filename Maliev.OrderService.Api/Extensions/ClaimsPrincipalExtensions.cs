using System.Security.Claims;

namespace Maliev.OrderService.Api.Extensions;

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
}

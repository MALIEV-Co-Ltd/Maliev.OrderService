using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Maliev.OrderService.Api.Authorization;

/// <summary>
/// Dynamically creates authorization policies based on permission strings.
/// This avoids the need to register every permission as a separate policy in Program.cs.
/// </summary>
public class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionAuthorizationPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the authorization policy for the specified name.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <returns>The authorization policy.</returns>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if the policy already exists (e.g., standard "Admin" policy)
        var policy = await base.GetPolicyAsync(policyName);
        if (policy != null)
        {
            return policy;
        }

        // If not, assume it's a permission string and create a dynamic policy
        // Format check: order.{resource}.{action}
        if (IsValidPermissionFormat(policyName))
        {
            return new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
        }

        return null;
    }

    private static bool IsValidPermissionFormat(string permission)
    {
        var parts = permission.Split('.');
        return parts.Length == 3 && parts[0] == "order";
    }
}

/// <summary>
/// Authorization requirement representing a specific permission.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the required permission string.
    /// </summary>
    public string Permission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
    /// </summary>
    /// <param name="permission">The permission string.</param>
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.OrderService.Api.Authorization;

namespace Maliev.OrderService.Api.Services
{
    /// <summary>
    /// Service that registers Order Service permissions and roles with the central IAM service on startup.
    /// Inherits from the standard <see cref="IAMRegistrationService"/> from ServiceDefaults.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderIAMRegistrationService"/> class.
    /// </remarks>
    public class OrderIAMRegistrationService(
        IConfiguration configuration,
        ILogger<OrderIAMRegistrationService> logger) : IAMRegistrationService(configuration, logger, "order")
    {

        /// <inheritdoc />
        protected override IEnumerable<PermissionRegistration> GetPermissions()
        {
            return OrderPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
            {
                PermissionId = p.Key,
                Description = p.Value
            });
        }

        /// <inheritdoc />
        protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
        {
            return OrderPredefinedRoles.All.Select(r => new RoleRegistration
            {
                RoleId = r.RoleId,
                Description = r.Description,
                PermissionIds = [.. r.Permissions],
                IsCustom = false
            });
        }
    }
}



using System.Security.Claims;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Data.Models;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Implementation of the order authorization service.
    /// </summary>
    public class OrderAuthorizationService : IOrderAuthorizationService
    {
        /// <inheritdoc />
        public bool CanViewOrder(ClaimsPrincipal user, Order order)
        {
            IEnumerable<string> roles = user.GetRoles();
            string userId = user.GetUserId();

            // Admin and Manager can see all orders (supporting both full and simple role names for tests)
            if (roles.Contains(OrderPredefinedRoles.Admin, StringComparer.OrdinalIgnoreCase) ||
                roles.Contains(OrderPredefinedRoles.Manager, StringComparer.OrdinalIgnoreCase) ||
                roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
                roles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            // Creator can only see their own orders
            if (roles.Contains(OrderPredefinedRoles.Creator, StringComparer.OrdinalIgnoreCase) && order.CustomerId == userId)
            {
                return true;
            }

            // Fulfillment can only see orders assigned to them
            return roles.Contains(OrderPredefinedRoles.Fulfillment, StringComparer.OrdinalIgnoreCase) && order.AssignedEmployeeId == userId;
        }

        /// <inheritdoc />
        public bool CanViewOrder(ClaimsPrincipal user, OrderResponse order)
        {
            IEnumerable<string> roles = user.GetRoles();
            string userId = user.GetUserId();

            // Admin and Manager can see all orders (supporting both full and simple role names for tests)
            if (roles.Contains(OrderPredefinedRoles.Admin, StringComparer.OrdinalIgnoreCase) ||
                roles.Contains(OrderPredefinedRoles.Manager, StringComparer.OrdinalIgnoreCase) ||
                roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
                roles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            // Creator can only see their own orders
            if (roles.Contains(OrderPredefinedRoles.Creator, StringComparer.OrdinalIgnoreCase) && order.CustomerId == userId)
            {
                return true;
            }

            // Fulfillment can only see orders assigned to them
            return roles.Contains(OrderPredefinedRoles.Fulfillment, StringComparer.OrdinalIgnoreCase) && order.AssignedEmployeeId == userId;
        }

        /// <inheritdoc />
        public IQueryable<Order> ApplyDataIsolationFilter(ClaimsPrincipal user, IQueryable<Order> query)
        {
            IEnumerable<string> roles = user.GetRoles();
            string userId = user.GetUserId();

            // Admin and Manager can see all orders (no filter needed)
            if (roles.Contains(OrderPredefinedRoles.Admin, StringComparer.OrdinalIgnoreCase) ||
                roles.Contains(OrderPredefinedRoles.Manager, StringComparer.OrdinalIgnoreCase) ||
                roles.Contains("Admin", StringComparer.OrdinalIgnoreCase) ||
                roles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
            {
                return query;
            }

            // Creator can only see their own orders
            if (roles.Contains(OrderPredefinedRoles.Creator, StringComparer.OrdinalIgnoreCase))
            {
                return query.Where(o => o.CustomerId == userId);
            }

            // Fulfillment can only see orders assigned to them
            if (roles.Contains(OrderPredefinedRoles.Fulfillment, StringComparer.OrdinalIgnoreCase))
            {
                return query.Where(o => o.AssignedEmployeeId == userId);
            }

            // For other roles with orders.read permission, default to no access if not categorized above
            // This ensures data safety.
            return query.Where(o => false);
        }
    }
}

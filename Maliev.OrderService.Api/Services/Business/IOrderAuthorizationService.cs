using System.Security.Claims;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Api.Services.Business
{
    /// <summary>
    /// Service for handling complex authorization logic for orders.
    /// </summary>
    public interface IOrderAuthorizationService
    {
        /// <summary>
        /// Checks if the user can view a specific order.
        /// </summary>
        /// <param name="user">The claims principal.</param>
        /// <param name="order">The order to check.</param>
        /// <returns>True if the user can view the order; otherwise, false.</returns>
        bool CanViewOrder(ClaimsPrincipal user, Order order);

        /// <summary>
        /// Checks if the user can view a specific order DTO.
        /// </summary>
        /// <param name="user">The claims principal.</param>
        /// <param name="order">The order response DTO to check.</param>
        /// <returns>True if the user can view the order; otherwise, false.</returns>
        bool CanViewOrder(ClaimsPrincipal user, DTOs.Response.OrderResponse order);

        /// <summary>
        /// Applies data isolation filters to an order query based on user roles.
        /// </summary>
        /// <param name="user">The claims principal.</param>
        /// <param name="query">The order query to filter.</param>
        /// <returns>The filtered query.</returns>
        IQueryable<Order> ApplyDataIsolationFilter(ClaimsPrincipal user, IQueryable<Order> query);
    }
}

using Asp.Versioning;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Api.Authorization;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing order status transitions
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderStatusController"/> class
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("order/v{version:apiVersion}/orders/{orderId}/statuses")]
    [Produces("application/json")]
    public class OrderStatusController(
        IOrderStatusService statusService,
        IAuthorizationService authorizationService,
        ILogger<OrderStatusController> logger) : ControllerBase
    {
        private readonly IOrderStatusService _statusService = statusService;
        private readonly IAuthorizationService _authorizationService = authorizationService; // Added
        private readonly ILogger<OrderStatusController> _logger = logger;

        /// <summary>
        /// Create a new status entry for an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">The status creation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created status or error if order not found</returns>
        [HttpPost]
        [RequirePermission(OrderPermissions.OrdersUpdate)] // Base permission
        public async Task<IActionResult> CreateOrderStatus(
            string orderId,
            [FromBody] CreateOrderStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Status-specific permission checks
            if (string.Equals(request.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                if (!(await _authorizationService.AuthorizeAsync(User, "Permission:" + OrderPermissions.OrdersApprove)).Succeeded)
                {
                    return Forbid();
                }
            }
            else if (string.Equals(request.Status, "Finished", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(request.Status, "Shipped", StringComparison.OrdinalIgnoreCase))
            {
                if (!(await _authorizationService.AuthorizeAsync(User, "Permission:" + OrderPermissions.OrdersFulfill)).Succeeded)
                {
                    return Forbid();
                }
            }
            else if (string.Equals(request.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                if (!(await _authorizationService.AuthorizeAsync(User, "Permission:" + OrderPermissions.OrdersCancel)).Succeeded)
                {
                    return Forbid();
                }
            }

            try
            {
                string updatedBy = User.GetUserId();
                OrderStatusResponse status = await _statusService.CreateOrderStatusAsync(orderId, request, updatedBy, cancellationToken);
                return CreatedAtRoute(new { orderId }, status);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

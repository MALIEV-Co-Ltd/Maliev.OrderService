using Asp.Versioning;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Maliev.OrderService.Api.Extensions;

namespace Maliev.OrderService.Api.Controllers;

/// <summary>
/// Controller for managing order status transitions
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("orders/v{version:apiVersion}/orders/{orderId}/statuses")]
[Authorize(Policy = "EmployeeOrHigher")]
[EnableRateLimiting("general")]
public class OrderStatusController : ControllerBase
{
    private readonly IOrderStatusService _statusService;
    private readonly ILogger<OrderStatusController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderStatusController"/> class
    /// </summary>
    public OrderStatusController(
        IOrderStatusService statusService,
        ILogger<OrderStatusController> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new status entry for an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="request">The status creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created status or error if order not found</returns>
    [HttpPost]
    public async Task<IActionResult> CreateOrderStatus(
        string orderId,
        [FromBody] CreateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updatedBy = User.GetUserId();
            var status = await _statusService.CreateOrderStatusAsync(orderId, request, updatedBy, cancellationToken);
            return CreatedAtRoute(new { orderId }, status);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

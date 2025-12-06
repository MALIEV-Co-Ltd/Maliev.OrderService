using Asp.Versioning;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Controllers;

/// <summary>
/// Controller for managing orders
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("orders/v{version:apiVersion}/orders")]
[Authorize]
[EnableRateLimiting("general")]
public class OrdersController : ControllerBase
{
    private readonly IOrderManagementService _orderService;
    private readonly IOrderStatusService _statusService;
    private readonly IOrderFileService _fileService;
    private readonly IOrderNoteService _noteService;
    private readonly ILogger<OrdersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrdersController"/> class
    /// </summary>
    public OrdersController(
        IOrderManagementService orderService,
        IOrderStatusService statusService,
        IOrderFileService fileService,
        IOrderNoteService noteService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _statusService = statusService;
        _fileService = fileService;
        _noteService = noteService;
        _logger = logger;
    }

    /// <summary>
    /// Get a paginated list of orders
    /// </summary>
    /// <param name="page">The page number (default: 1)</param>
    /// <param name="pageSize">The number of items per page (default: 20)</param>
    /// <param name="customerId">Optional filter by customer ID</param>
    /// <param name="status">Optional filter by order status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A paginated list of orders</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersAsync(page, pageSize, customerId, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get an order by its ID
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The order details or 404 if not found</returns>
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderById(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            return NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" });
        }

        return Ok(order);
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="request">The order creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created order</returns>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdBy = "system"; // TODO: Get from user context after authentication
        var order = await _orderService.CreateOrderAsync(request, createdBy, cancellationToken);

        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, order);
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="request">The order update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated order or error if not found/conflict</returns>
    [HttpPut("{orderId}")]
    public async Task<IActionResult> UpdateOrder(
        string orderId,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var updatedBy = "system"; // TODO: Get from user context
            var order = await _orderService.UpdateOrderAsync(orderId, request, updatedBy, cancellationToken);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ErrorMessageResponse { Message = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Conflict(new ErrorMessageResponse { Message = "Order has been modified by another user. Please refresh and try again." });
        }
    }

    /// <summary>
    /// Cancel an order (soft delete)
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message or 404 if not found</returns>
    [HttpDelete("{orderId}")]
    public async Task<IActionResult> CancelOrder(string orderId, CancellationToken cancellationToken = default)
    {
        var cancelledBy = "system"; // TODO: Get from user context
        var result = await _orderService.CancelOrderAsync(orderId, cancelledBy, cancellationToken: cancellationToken);

        if (!result)
        {
            return NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" });
        }

        return Ok(new SuccessMessageResponse { Message = "Order cancelled successfully" });
    }

    /// <summary>
    /// Cancel an order with a specific reason
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="request">The cancellation request with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message with reason or 404 if not found</returns>
    [HttpPost("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrderWithReason(
        string orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var cancelledBy = "system"; // TODO: Get from user context
        var result = await _orderService.CancelOrderAsync(orderId, cancelledBy, request.CancellationReason, cancellationToken);

        if (!result)
        {
            return NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" });
        }

        return Ok(new SuccessMessageWithReasonResponse { Message = "Order cancelled successfully", Reason = request.CancellationReason });
    }

    /// <summary>
    /// Get status history for an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of order statuses</returns>
    [HttpGet("{orderId}/statuses")]
    public async Task<IActionResult> GetOrderStatuses(string orderId, CancellationToken cancellationToken = default)
    {
        var statuses = await _statusService.GetOrderStatusHistoryAsync(orderId, cancellationToken);
        return Ok(statuses);
    }

    /// <summary>
    /// Get all files associated with an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of order files</returns>
    [HttpGet("{orderId}/files")]
    public async Task<IActionResult> GetOrderFiles(string orderId, CancellationToken cancellationToken = default)
    {
        var files = await _fileService.GetOrderFilesAsync(orderId, cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Get all notes associated with an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of order notes</returns>
    [HttpGet("{orderId}/notes")]
    public async Task<IActionResult> GetOrderNotes(string orderId, CancellationToken cancellationToken = default)
    {
        var notes = await _noteService.GetOrderNotesAsync(orderId, cancellationToken);
        return Ok(notes);
    }
}
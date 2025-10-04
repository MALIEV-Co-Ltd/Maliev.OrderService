using Asp.Versioning;
using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.OrderService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/orders")]
[Authorize]
[EnableRateLimiting("general")]
public class OrdersController : ControllerBase
{
    private readonly IOrderManagementService _orderService;
    private readonly IOrderStatusService _statusService;
    private readonly IOrderFileService _fileService;
    private readonly IOrderNoteService _noteService;
    private readonly IValidator<CreateOrderRequest> _createOrderValidator;
    private readonly IValidator<UpdateOrderRequest> _updateOrderValidator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderManagementService orderService,
        IOrderStatusService statusService,
        IOrderFileService fileService,
        IOrderNoteService noteService,
        IValidator<CreateOrderRequest> createOrderValidator,
        IValidator<UpdateOrderRequest> updateOrderValidator,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _statusService = statusService;
        _fileService = fileService;
        _noteService = noteService;
        _createOrderValidator = createOrderValidator;
        _updateOrderValidator = updateOrderValidator;
        _logger = logger;
    }

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

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderById(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            return NotFound(new { message = $"Order {orderId} not found" });
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var createdBy = "system"; // TODO: Get from user context after authentication
        var order = await _orderService.CreateOrderAsync(request, createdBy, cancellationToken);

        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, order);
    }

    [HttpPut("{orderId}")]
    public async Task<IActionResult> UpdateOrder(
        string orderId,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        try
        {
            var updatedBy = "system"; // TODO: Get from user context
            var order = await _orderService.UpdateOrderAsync(orderId, request, updatedBy, cancellationToken);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Order has been modified by another user. Please refresh and try again." });
        }
    }

    [HttpDelete("{orderId}")]
    public async Task<IActionResult> CancelOrder(string orderId, CancellationToken cancellationToken = default)
    {
        var cancelledBy = "system"; // TODO: Get from user context
        var result = await _orderService.CancelOrderAsync(orderId, cancelledBy, cancellationToken: cancellationToken);

        if (!result)
        {
            return NotFound(new { message = $"Order {orderId} not found" });
        }

        return Ok(new { message = "Order cancelled successfully" });
    }

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
            return NotFound(new { message = $"Order {orderId} not found" });
        }

        return Ok(new { message = "Order cancelled successfully", reason = request.CancellationReason });
    }

    [HttpGet("{orderId}/statuses")]
    public async Task<IActionResult> GetOrderStatuses(string orderId, CancellationToken cancellationToken = default)
    {
        var statuses = await _statusService.GetOrderStatusHistoryAsync(orderId, cancellationToken);
        return Ok(statuses);
    }

    [HttpGet("{orderId}/files")]
    public async Task<IActionResult> GetOrderFiles(string orderId, CancellationToken cancellationToken = default)
    {
        var files = await _fileService.GetOrderFilesAsync(orderId, cancellationToken);
        return Ok(files);
    }

    [HttpGet("{orderId}/notes")]
    public async Task<IActionResult> GetOrderNotes(string orderId, CancellationToken cancellationToken = default)
    {
        var notes = await _noteService.GetOrderNotesAsync(orderId, cancellationToken);
        return Ok(notes);
    }
}

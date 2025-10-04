using Asp.Versioning;
using FluentValidation;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.OrderService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/orders/batch")]
[Authorize(Policy = "EmployeeOrHigher")]
[EnableRateLimiting("batch")]
public partial class BatchOrdersController : ControllerBase
{
    private readonly IOrderManagementService _orderService;
    private readonly OrderDbContext _context;
    private readonly IValidator<CreateOrderRequest> _createValidator;
    private readonly IValidator<UpdateOrderRequest> _updateValidator;
    private readonly ILogger<BatchOrdersController> _logger;

    public BatchOrdersController(
        IOrderManagementService orderService,
        OrderDbContext context,
        IValidator<CreateOrderRequest> createValidator,
        IValidator<UpdateOrderRequest> updateValidator,
        ILogger<BatchOrdersController> logger)
    {
        _orderService = orderService;
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBatchOrders(
        [FromBody] CreateOrderRequest[] requests,
        CancellationToken cancellationToken = default)
    {
        // Validate all requests first
        for (int i = 0; i < requests.Length; i++)
        {
            var validationResult = await _createValidator.ValidateAsync(requests[i], cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = $"Validation failed for item at index {i}",
                    errors = validationResult.Errors,
                    itemIndex = i
                });
            }
        }

        // Use transaction for all-or-nothing
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var results = new List<object>();
            var createdBy = "system"; // TODO: Get from user context

            foreach (var request in requests)
            {
                var order = await _orderService.CreateOrderAsync(request, createdBy, cancellationToken);
                results.Add(order);
            }

            await transaction.CommitAsync(cancellationToken);
            return Created("/v1/orders/batch", results);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchOrderCreationFailed(_logger, ex);
            return StatusCode(500, new { message = "Batch order creation failed", error = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateBatchOrders(
        [FromBody] BatchUpdateOrderRequest[] requests,
        CancellationToken cancellationToken = default)
    {
        // Validate all requests first
        for (int i = 0; i < requests.Length; i++)
        {
            var updateRequest = new UpdateOrderRequest
            {
                Version = requests[i].Version,
                AssignedEmployeeId = requests[i].AssignedEmployeeId
            };

            var validationResult = await _updateValidator.ValidateAsync(updateRequest, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = $"Validation failed for item at index {i}",
                    errors = validationResult.Errors,
                    itemIndex = i
                });
            }
        }

        // Use transaction for all-or-nothing
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var results = new List<object>();
            var updatedBy = "system"; // TODO: Get from user context

            foreach (var request in requests)
            {
                var updateRequest = new UpdateOrderRequest
                {
                    Version = request.Version,
                    AssignedEmployeeId = request.AssignedEmployeeId
                };

                var order = await _orderService.UpdateOrderAsync(request.OrderId, updateRequest, updatedBy, cancellationToken);
                results.Add(order);
            }

            await transaction.CommitAsync(cancellationToken);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchOrderUpdateFailed(_logger, ex);
            return BadRequest(new { message = "Batch order update failed", error = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchUpdateConcurrencyConflict(_logger, ex);
            return BadRequest(new { message = "One or more orders have been modified by another user", error = ex.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchOrderUpdateFailed(_logger, ex);
            return StatusCode(500, new { message = "Batch order update failed", error = ex.Message });
        }
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelBatchOrders(
        [FromBody] string[] orderIds,
        CancellationToken cancellationToken = default)
    {
        if (orderIds == null || orderIds.Length == 0)
        {
            return BadRequest(new { message = "Order IDs are required" });
        }

        // Use transaction for all-or-nothing
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var cancelledBy = "system"; // TODO: Get from user context
            var results = new List<object>();

            foreach (var orderId in orderIds)
            {
                var result = await _orderService.CancelOrderAsync(orderId, cancelledBy, cancellationToken: cancellationToken);
                if (!result)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return NotFound(new { message = $"Order {orderId} not found" });
                }
                results.Add(new { orderId, cancelled = true });
            }

            await transaction.CommitAsync(cancellationToken);
            return Ok(new { message = $"{orderIds.Length} orders cancelled successfully", results });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchOrderCancellationFailed(_logger, ex);
            return StatusCode(500, new { message = "Batch order cancellation failed", error = ex.Message });
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Batch order creation failed, transaction rolled back")]
        public static partial void BatchOrderCreationFailed(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Batch update failed due to concurrency conflict")]
        public static partial void BatchUpdateConcurrencyConflict(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Batch order update failed, transaction rolled back")]
        public static partial void BatchOrderUpdateFailed(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Batch order cancellation failed, transaction rolled back")]
        public static partial void BatchOrderCancellationFailed(ILogger logger, Exception ex);
    }
}

// DTO for batch update request
public class BatchUpdateOrderRequest
{
    public required string OrderId { get; set; }
    public required string Version { get; set; }
    public string? AssignedEmployeeId { get; set; }
}

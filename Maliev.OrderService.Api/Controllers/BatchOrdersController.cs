using Asp.Versioning;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Maliev.OrderService.Api.DTOs.Response;
using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Controllers;

/// <summary>
/// Controller for batch operations on orders
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("orders/v{version:apiVersion}/orders/batch")]
[Authorize(Policy = "EmployeeOrHigher")]
[EnableRateLimiting("batch")]
public partial class BatchOrdersController : ControllerBase
{
    private readonly IOrderManagementService _orderService;
    private readonly OrderDbContext _context;
    private readonly ILogger<BatchOrdersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchOrdersController"/> class
    /// </summary>
    public BatchOrdersController(
        IOrderManagementService orderService,
        OrderDbContext context,
        ILogger<BatchOrdersController> logger)
    {
        _orderService = orderService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create multiple orders in a single transaction
    /// </summary>
    /// <param name="requests">Array of order creation requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of created orders or error if any validation fails</returns>
    [HttpPost]
    public async Task<IActionResult> CreateBatchOrders(
        [FromBody] CreateOrderRequest[] requests,
        CancellationToken cancellationToken = default)
    {
        // Validate all requests first using DataAnnotations
        for (int i = 0; i < requests.Length; i++)
        {
            var validationContext = new ValidationContext(requests[i]);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(requests[i], validationContext, validationResults, true))
            {
                return BadRequest(new BatchValidationErrorResponse
                {
                    Message = $"Validation failed for item at index {i}",
                    Errors = validationResults.Select(r => new { ErrorMessage = r.ErrorMessage, MemberNames = r.MemberNames }).ToList(),
                    ItemIndex = i
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
            return StatusCode(500, new ErrorMessageResponse { Message = "Batch order creation failed", Error = ex.Message });
        }
    }

    /// <summary>
    /// Update multiple orders in a single transaction
    /// </summary>
    /// <param name="requests">Array of order update requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of updated orders or error if any validation/concurrency fails</returns>
    [HttpPut]
    public async Task<IActionResult> UpdateBatchOrders(
        [FromBody] BatchUpdateOrderRequest[] requests,
        CancellationToken cancellationToken = default)
    {
        // Validate all requests first using DataAnnotations
        for (int i = 0; i < requests.Length; i++)
        {
            var updateRequest = new UpdateOrderRequest
            {
                Version = requests[i].Version,
                AssignedEmployeeId = requests[i].AssignedEmployeeId
            };

            var validationContext = new ValidationContext(updateRequest);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(updateRequest, validationContext, validationResults, true))
            {
                return BadRequest(new BatchValidationErrorResponse
                {
                    Message = $"Validation failed for item at index {i}",
                    Errors = validationResults.Select(r => new { ErrorMessage = r.ErrorMessage, MemberNames = r.MemberNames }).ToList(),
                    ItemIndex = i
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
            return BadRequest(new ErrorMessageResponse { Message = "Batch order update failed", Error = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchUpdateConcurrencyConflict(_logger, ex);
            return BadRequest(new ErrorMessageResponse { Message = "One or more orders have been modified by another user", Error = ex.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchOrderUpdateFailed(_logger, ex);
            return StatusCode(500, new ErrorMessageResponse { Message = "Batch order update failed", Error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel multiple orders in a single transaction
    /// </summary>
    /// <param name="orderIds">Array of order IDs to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message with cancellation results or error if any order not found</returns>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelBatchOrders(
        [FromBody] string[] orderIds,
        CancellationToken cancellationToken = default)
    {
        if (orderIds == null || orderIds.Length == 0)
        {
            return BadRequest(new ErrorMessageResponse { Message = "Order IDs are required" });
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
                    return NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" });
                }
                results.Add(new { orderId, cancelled = true });
            }

            await transaction.CommitAsync(cancellationToken);
            return Ok(new SuccessBatchCancellationResponse { Message = $"{orderIds.Length} orders cancelled successfully", Results = results });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.BatchOrderCancellationFailed(_logger, ex);
            return StatusCode(500, new ErrorMessageResponse { Message = "Batch order cancellation failed", Error = ex.Message });
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
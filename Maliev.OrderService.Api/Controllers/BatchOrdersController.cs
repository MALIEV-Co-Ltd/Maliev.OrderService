using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Api.Mapping;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Infrastructure.Persistence;
using Maliev.OrderService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for batch operations on orders
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="BatchOrdersController"/> class
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("order/v{version:apiVersion}/orders/batch")]
    [EnableRateLimiting(RateLimitPolicies.Batch)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public partial class BatchOrdersController(
        IOrderManagementService orderService,
        IOrderAuthorizationService orderAuthService,
        OrderDbContext context,
        ILogger<BatchOrdersController> logger) : ControllerBase
    {
        private readonly IOrderManagementService _orderService = orderService;
        private readonly IOrderAuthorizationService _orderAuthService = orderAuthService;
        private readonly OrderDbContext _context = context;
        private readonly ILogger<BatchOrdersController> _logger = logger;

        /// <summary>
        /// Create multiple orders in a single transaction
        /// </summary>
        /// <param name="requests">Array of order creation requests</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of created orders or error if any validation fails</returns>
        [HttpPost]
        [RequirePermission(OrderPermissions.OrdersCreate)]
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
                        Errors = validationResults.Select(r => new { r.ErrorMessage, r.MemberNames }).ToList(),
                        ItemIndex = i
                    });
                }
            }

            // Use execution strategy for retry logic with transactions
            IExecutionStrategy strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Use transaction for all-or-nothing
                using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var orders = new List<Order>();
                    string createdBy = User.GetUserId();

                    foreach (CreateOrderRequest request in requests)
                    {
                        Order order = await _orderService.PrepareOrderEntityForCreationAsync(request, createdBy, cancellationToken);
                        orders.Add(order);
                    }

                    // Save all changes in one go
                    _ = await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Map results AFTER save to get correct xmin
                    var results = orders.Select(o => o.ToOrderResponse(_context.Entry(o).Property<uint>("xmin").CurrentValue)).ToList();

                    return Created(uri: (string?)null, new { Message = $"{requests.Length} orders created successfully", Results = results });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    Log.BatchOrderCreationFailed(_logger, ex);
                    return StatusCode(500, new ErrorMessageResponse { Message = "Batch order creation failed", Error = ex.Message });
                }
            });
        }

        /// <summary>
        /// Update multiple orders in a single transaction
        /// </summary>
        /// <param name="requests">Array of order update requests</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of updated orders or error if any validation/concurrency fails</returns>
        [HttpPut]
        [RequirePermission(OrderPermissions.OrdersUpdate)]
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
                    AssignedEmployeeId = requests[i].AssignedEmployeeId,
                    Requirements = requests[i].Requirements,
                    OrderedQuantity = requests[i].OrderedQuantity,
                    DepartmentId = requests[i].DepartmentId
                };

                var validationContext = new ValidationContext(updateRequest);
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(updateRequest, validationContext, validationResults, true))
                {
                    return BadRequest(new BatchValidationErrorResponse
                    {
                        Message = $"Validation failed for item at index {i}",
                        Errors = validationResults.Select(r => new { r.ErrorMessage, r.MemberNames }).ToList(),
                        ItemIndex = i
                    });
                }
            }

            foreach (BatchUpdateOrderRequest request in requests)
            {
                IActionResult? accessError = await OrderAccessGuard.EnsureCanAccessOrderAsync(
                    this,
                    _context,
                    _orderAuthService,
                    request.OrderId,
                    cancellationToken,
                    notFoundAsBadRequest: true);
                if (accessError != null)
                {
                    return accessError;
                }
            }

            // Use execution strategy for retry logic with transactions
            IExecutionStrategy strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Use transaction for all-or-nothing
                using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var results = new List<OrderResponse>();
                    string updatedBy = User.GetUserId();

                    foreach (BatchUpdateOrderRequest request in requests)
                    {
                        var updateRequest = new UpdateOrderRequest
                        {
                            Version = request.Version,
                            AssignedEmployeeId = request.AssignedEmployeeId,
                            Requirements = request.Requirements,
                            OrderedQuantity = request.OrderedQuantity,
                            DepartmentId = request.DepartmentId
                        };

                        OrderResponse updatedOrderDto = await _orderService.UpdateOrderAsync(request.OrderId, updateRequest, updatedBy, cancellationToken);
                        results.Add(updatedOrderDto);
                    }

                    await transaction.CommitAsync(cancellationToken);
                    return Ok(new { Message = $"{requests.Length} orders updated successfully", Results = results });
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    Log.BatchOrderUpdateFailed(_logger, ex);
                    return BadRequest(new ErrorMessageResponse { Message = "Batch order update failed", Error = ex.Message });
                }
                catch (DbUpdateConcurrencyException ex)
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
            });
        }

        /// <summary>
        /// Cancel multiple orders in a single transaction
        /// </summary>
        /// <param name="orderIds">Array of order IDs to cancel</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message with cancellation results or error if any order not found</returns>
        [HttpPost("cancel")]
        [RequirePermission(OrderPermissions.OrdersCancel)]
        public async Task<IActionResult> CancelBatchOrders(
            [FromBody] string[] orderIds,
            CancellationToken cancellationToken = default)
        {
            if (orderIds == null || orderIds.Length == 0)
            {
                return BadRequest(new ErrorMessageResponse { Message = "Order IDs are required" });
            }

            foreach (string orderId in orderIds)
            {
                IActionResult? accessError = await OrderAccessGuard.EnsureCanAccessOrderAsync(
                    this,
                    _context,
                    _orderAuthService,
                    orderId,
                    cancellationToken);
                if (accessError != null)
                {
                    return accessError;
                }
            }

            // Use execution strategy for retry logic with transactions
            IExecutionStrategy strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // Use transaction for all-or-nothing
                using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    string cancelledBy = User.GetUserId();
                    var results = new List<object>();

                    foreach (string orderId in orderIds)
                    {
                        bool result = await _orderService.CancelOrderAsync(orderId, cancelledBy, cancellationToken: cancellationToken);
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
            });
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
}

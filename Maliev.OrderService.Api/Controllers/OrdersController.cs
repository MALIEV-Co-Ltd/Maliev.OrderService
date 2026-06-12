using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Domain.Entities;
using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing orders
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrdersController"/> class
    /// </remarks>
    [ApiController]
    [ApiVersion("1")]
    [Route("order/v{version:apiVersion}/orders")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    public partial class OrdersController(
        IOrderManagementService orderService,
        IOrderAuthorizationService orderAuthService,
        OrderDbContext context,
        IOrderStatusService statusService,
        IOrderFileService fileService,
        IOrderNoteService noteService,
        IOrderPreviewImageService previewImageService,
        IAuthorizationService authorizationService,
        ILogger<OrdersController> logger) : ControllerBase
    {
        private readonly IOrderManagementService _orderService = orderService;
        private readonly IOrderAuthorizationService _orderAuthService = orderAuthService;
        private readonly OrderDbContext _context = context;
        private readonly IOrderStatusService _statusService = statusService;
        private readonly IOrderFileService _fileService = fileService;
        private readonly IOrderNoteService _noteService = noteService;
        private readonly IOrderPreviewImageService _previewImageService = previewImageService;
        private readonly IAuthorizationService _authorizationService = authorizationService; // Added
        private readonly ILogger<OrdersController> _logger = logger;

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
        [RequirePermission(OrderPermissions.OrdersRead)]
        public async Task<IActionResult> GetOrders(
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20,
            [FromQuery] string? customerId = null,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            PaginatedResponse<OrderResponse> result = await _orderService.GetOrdersAsync(page, pageSize, User, customerId, status, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get an order by its ID
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The order details or 404 if not found</returns>
        [HttpGet("{orderId}")]
        [RequirePermission(OrderPermissions.OrdersRead)]
        public async Task<IActionResult> GetOrderById(string orderId, CancellationToken cancellationToken = default)
        {
            OrderResponse? order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);
            if (order == null)
            {
                return NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" });
            }

            // Data Isolation Check: delegate to auth service
            if (!_orderAuthService.CanViewOrder(User, order))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    string userId = User.GetUserId();
                    Log.UnauthorizedOrderAccess(_logger, userId, orderId);
                }
                return Forbid();
            }

            return Ok(order);
        }

        /// <summary>
        /// Get production line items for an order.
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The production line items for the order</returns>
        [HttpGet("{orderId}/items")]
        [RequirePermission(OrderPermissions.LineItemsRead)]
        public async Task<IActionResult> GetOrderItems(string orderId, CancellationToken cancellationToken = default)
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

            Order? order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.ServiceCategory)
                .Include(o => o.ProcessType)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
            if (order == null)
            {
                return NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" });
            }

            var item = new OrderLineItemResponse
            {
                OrderItemId = StableGuid($"order-item:{order.OrderId}:1"),
                MaterialId = order.MaterialId.HasValue
                    ? StableGuid($"material:{order.MaterialId.Value}")
                    : Guid.Empty,
                MaterialSnapshotJson = BuildMaterialSnapshotJson(order),
                ConfigurationSnapshotJson = BuildConfigurationSnapshotJson(order),
                Technology = order.ProcessType?.Name ?? order.ServiceCategory.Name,
                VolumeCm3 = 0,
                Quantity = Math.Max(1, order.OrderedQuantity ?? 1),
                EstimatedPrintTimeMinutes = 0,
                DeliveryDate = order.PromisedDeliveryDate
            };

            return Ok(new[] { item });
        }

        private static string BuildMaterialSnapshotJson(Order order)
        {
            return JsonSerializer.Serialize(new
            {
                materialId = order.MaterialId,
                materialName = order.MaterialName,
                colorId = order.ColorId,
                colorName = order.ColorName,
                surfaceFinishingId = order.SurfaceFinishingId,
                surfaceFinishingName = order.SurfaceFinishingName,
                materialCacheUpdatedAt = order.MaterialCacheUpdatedAt
            });
        }

        private static string BuildConfigurationSnapshotJson(Order order)
        {
            return JsonSerializer.Serialize(new
            {
                orderId = order.OrderId,
                serviceCategoryId = order.ServiceCategoryId,
                serviceCategoryName = order.ServiceCategory.Name,
                processTypeId = order.ProcessTypeId,
                processTypeName = order.ProcessType?.Name,
                orderedQuantity = Math.Max(1, order.OrderedQuantity ?? 1),
                leadTimeDays = order.LeadTimeDays,
                promisedDeliveryDate = order.PromisedDeliveryDate,
                requirements = order.Requirements,
                isConfidential = order.IsConfidential,
                isOutsourced = order.IsOutsourced,
                quotedAmount = order.QuotedAmount,
                quoteCurrency = order.QuoteCurrency
            });
        }

        /// <summary>
        /// Create a new order
        /// </summary>
        /// <param name="request">The order creation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created order</returns>
        [HttpPost]
        [RequirePermission(OrderPermissions.OrdersCreate)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string createdBy = User.GetUserId();
            OrderResponse order = await _orderService.CreateOrderAsync(request, createdBy, cancellationToken);

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
        [RequirePermission(OrderPermissions.OrdersUpdate)]
        public async Task<IActionResult> UpdateOrder(
            string orderId,
            [FromBody] UpdateOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            // Field-level check: Pricing updates require higher permissions
            if (request.QuotedAmount.HasValue || !string.IsNullOrEmpty(request.QuoteCurrency))
            {
                if (!(await _authorizationService.AuthorizeAsync(User, "Permission:" + OrderPermissions.OrdersApprove)).Succeeded)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        string userId = User.GetUserId();
                        Log.UnauthorizedPricingUpdate(_logger, userId, orderId);
                    }
                    return Forbid();
                }
            }

            try
            {
                string updatedBy = User.GetUserId();
                OrderResponse order = await _orderService.UpdateOrderAsync(orderId, request, updatedBy, cancellationToken);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message.Contains("version", StringComparison.OrdinalIgnoreCase)
                    ? BadRequest(new ErrorMessageResponse { Message = ex.Message })
                    : NotFound(new ErrorMessageResponse { Message = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
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
        [RequirePermission(OrderPermissions.OrdersCancel)]
        public async Task<IActionResult> CancelOrder(string orderId, CancellationToken cancellationToken = default)
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

            string cancelledBy = User.GetUserId();
            bool result = await _orderService.CancelOrderAsync(orderId, cancelledBy, cancellationToken: cancellationToken);

            return !result
                ? NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" })
                : Ok(new SuccessMessageResponse { Message = "Order cancelled successfully" });
        }

        /// <summary>
        /// Cancel an order with a specific reason
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">The cancellation request with reason</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message with reason or 404 if not found</returns>
        [HttpPost("{orderId}/cancel")]
        [RequirePermission(OrderPermissions.OrdersCancel)]
        public async Task<IActionResult> CancelOrderWithReason(
            string orderId,
            [FromBody] CancelOrderRequest request,
            CancellationToken cancellationToken = default)
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

            string cancelledBy = User.GetUserId();
            bool result = await _orderService.CancelOrderAsync(orderId, cancelledBy, request.CancellationReason, cancellationToken);

            return !result
                ? NotFound(new ErrorMessageResponse { Message = $"Order {orderId} not found" })
                : Ok(new SuccessMessageWithReasonResponse { Message = "Order cancelled successfully", Reason = request.CancellationReason });
        }

        /// <summary>
        /// Get status history for an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of order statuses</returns>
        [HttpGet("{orderId}/statuses")]
        [RequirePermission(OrderPermissions.OrdersRead)]
        public async Task<IActionResult> GetOrderStatuses(string orderId, CancellationToken cancellationToken = default)
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

            List<OrderStatusResponse> statuses = await _statusService.GetOrderStatusHistoryAsync(orderId, cancellationToken);
            return Ok(statuses);
        }

        /// <summary>
        /// Get all files associated with an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of order files</returns>
        [HttpGet("{orderId}/files")]
        [RequirePermission(OrderPermissions.OrdersRead)]
        public async Task<IActionResult> GetOrderFiles(string orderId, CancellationToken cancellationToken = default)
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

            List<OrderFileResponse> files = await _fileService.GetOrderFilesAsync(orderId, cancellationToken);
            return Ok(files);
        }

        /// <summary>
        /// Get all notes associated with an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of order notes</returns>
        [HttpGet("{orderId}/notes")]
        [RequirePermission(OrderPermissions.OrdersRead)]
        public async Task<IActionResult> GetOrderNotes(string orderId, CancellationToken cancellationToken = default)
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

            List<OrderNoteResponse> notes = await _noteService.GetOrderNotesAsync(orderId, cancellationToken);
            return Ok(notes);
        }

        /// <summary>
        /// Get all preview images associated with an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of preview images</returns>
        [HttpGet("{orderId}/preview-images")]
        [RequirePermission(OrderPermissions.OrdersRead)]
        public async Task<IActionResult> GetOrderPreviewImages(string orderId, CancellationToken cancellationToken = default)
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

            List<OrderPreviewImageResponse> previewImages = await _previewImageService.GetOrderPreviewImagesAsync(orderId, cancellationToken);
            return Ok(previewImages);
        }

        /// <summary>
        /// Set a file as the primary 3D file for the order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="fileId">The file ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success or not found</returns>
        [HttpPut("{orderId}/files/{fileId}/set-primary")]
        [RequirePermission(OrderPermissions.OrdersUpdate)]
        public async Task<IActionResult> SetPrimaryFile(string orderId, long fileId, CancellationToken cancellationToken = default)
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

            bool result = await _fileService.SetPrimaryFileAsync(orderId, fileId, cancellationToken);
            return result ? Ok() : NotFound();
        }

        /// <summary>
        /// Updates the outsourcing status of an order.
        /// When marking as outsourced, all associated jobs are also flagged via event.
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">The outsourcing update request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated order</returns>
        [HttpPatch("{orderId}/outsourcing")]
        [RequirePermission(OrderPermissions.OrdersUpdate)]
        public async Task<IActionResult> UpdateOutsourcing(
            string orderId,
            [FromBody] UpdateOutsourcingRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            try
            {
                string actorId = User.GetUserId();
                OrderResponse order = await _orderService.UpdateOutsourcingAsync(orderId, request, actorId, cancellationToken);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new ErrorMessageResponse { Message = ex.Message });
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} attempted to access unauthorized order {OrderId}")]
            public static partial void UnauthorizedOrderAccess(ILogger logger, string userId, string orderId);

            [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} attempted unauthorized pricing update for order {OrderId}")]
            public static partial void UnauthorizedPricingUpdate(ILogger logger, string userId, string orderId);
        }

        private static Guid StableGuid(string value)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return new Guid(hash.AsSpan(0, 16));
        }
    }
}

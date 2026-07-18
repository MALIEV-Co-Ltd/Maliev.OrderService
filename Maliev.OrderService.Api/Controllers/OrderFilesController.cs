using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.OrderService.Api.Authorization;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing order file attachments
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderFilesController"/> class
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("order/v{version:apiVersion}/orders/{orderId}/files")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    public class OrderFilesController(
        IOrderAuthorizationService orderAuthService,
        OrderDbContext context,
        IOrderFileService fileService,
        ILogger<OrderFilesController> logger) : ControllerBase
    {
        private readonly IOrderAuthorizationService _orderAuthService = orderAuthService;
        private readonly OrderDbContext _context = context;
        private readonly IOrderFileService _fileService = fileService;
        private readonly ILogger<OrderFilesController> _logger = logger;

        /// <summary>
        /// Upload a file attachment to an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="request">The file upload request metadata</param>
        /// <param name="file">The file to upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The uploaded file metadata</returns>
        [HttpPost]
        [RequirePermission(OrderPermissions.OrdersUpdate)]
        public async Task<IActionResult> UploadOrderFile(
            string orderId,
            [FromForm] UploadOrderFileRequest request,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (file.Length == 0)
            {
                return BadRequest(new { message = "File is empty" });
            }

            if (file.Length > 100 * 1024 * 1024) // 100MB
            {
                return BadRequest(new { message = "File size exceeds 100MB limit" });
            }

            IActionResult? accessError = await OrderAccessGuard.EnsureCanAccessOrderAsync(
                this,
                _context,
                _orderAuthService,
                orderId,
                cancellationToken,
                notFoundAsBadRequest: true);
            if (accessError != null)
            {
                return accessError;
            }

            try
            {
                string uploadedBy = User.GetUserId();
                await using Stream stream = file.OpenReadStream();
                OrderFileResponse uploadedFile = await _fileService.UploadOrderFileAsync(
                    orderId,
                    request,
                    stream,
                    file.FileName,
                    uploadedBy,
                    cancellationToken);

                return CreatedAtRoute(new { orderId, fileId = uploadedFile.FileId }, uploadedFile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Download a file attachment from an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="fileId">The file ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The file stream or 404 if not found</returns>
        [HttpGet("{fileId}")]
        [RequirePermission(OrderPermissions.OrdersRead)]
        public async Task<IActionResult> DownloadOrderFile(
            string orderId,
            long fileId,
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

            (Stream? fileStream, string? fileName, string? contentType) = await _fileService.DownloadOrderFileAsync(orderId, fileId, cancellationToken);

            return fileStream == null ? NotFound(new { message = "File not found" }) : File(fileStream, contentType, fileName);
        }

        /// <summary>
        /// Delete a file attachment from an order
        /// </summary>
        /// <param name="orderId">The order ID</param>
        /// <param name="fileId">The file ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success message or 404 if not found</returns>
        [HttpDelete("{fileId}")]
        [RequirePermission(OrderPermissions.OrdersUpdate)]
        public async Task<IActionResult> DeleteOrderFile(
            string orderId,
            long fileId,
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

            bool result = await _fileService.DeleteOrderFileAsync(orderId, fileId, cancellationToken);

            if (!result)
            {
                return NotFound(new { message = "File not found" });
            }

            return Ok(new { message = "File deleted successfully" });
        }
    }
}

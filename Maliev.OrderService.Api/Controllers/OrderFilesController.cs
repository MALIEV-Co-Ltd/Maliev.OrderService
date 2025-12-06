using Asp.Versioning;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.OrderService.Api.Controllers;

/// <summary>
/// Controller for managing order file attachments
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("orders/v{version:apiVersion}/orders/{orderId}/files")]
[Authorize]
[EnableRateLimiting("general")]
public class OrderFilesController : ControllerBase
{
    private readonly IOrderFileService _fileService;
    private readonly ILogger<OrderFilesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderFilesController"/> class
    /// </summary>
    public OrderFilesController(
        IOrderFileService fileService,
        ILogger<OrderFilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file attachment to an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="request">The file upload request metadata</param>
    /// <param name="file">The file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The uploaded file metadata</returns>
    [HttpPost]
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

        try
        {
            var uploadedBy = "system"; // TODO: Get from user context
            await using var stream = file.OpenReadStream();
            var uploadedFile = await _fileService.UploadOrderFileAsync(
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
    public async Task<IActionResult> DownloadOrderFile(
        string orderId,
        long fileId,
        CancellationToken cancellationToken = default)
    {
        var (fileStream, fileName, contentType) = await _fileService.DownloadOrderFileAsync(orderId, fileId, cancellationToken);

        if (fileStream == null)
        {
            return NotFound(new { message = "File not found" });
        }

        return File(fileStream, contentType, fileName);
    }

    /// <summary>
    /// Delete a file attachment from an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="fileId">The file ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message or 404 if not found</returns>
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteOrderFile(
        string orderId,
        long fileId,
        CancellationToken cancellationToken = default)
    {
        var result = await _fileService.DeleteOrderFileAsync(orderId, fileId, cancellationToken);

        if (!result)
        {
            return NotFound(new { message = "File not found" });
        }

        return Ok(new { message = "File deleted successfully" });
    }
}

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
[Route("v{version:apiVersion}/orders/{orderId}/files")]
[Authorize]
[EnableRateLimiting("general")]
public class OrderFilesController : ControllerBase
{
    private readonly IOrderFileService _fileService;
    private readonly IValidator<UploadOrderFileRequest> _validator;
    private readonly ILogger<OrderFilesController> _logger;

    public OrderFilesController(
        IOrderFileService fileService,
        IValidator<UploadOrderFileRequest> validator,
        ILogger<OrderFilesController> logger)
    {
        _fileService = fileService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UploadOrderFile(
        string orderId,
        [FromForm] UploadOrderFileRequest request,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
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

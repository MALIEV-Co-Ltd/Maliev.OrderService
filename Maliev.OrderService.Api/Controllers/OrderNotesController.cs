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
[Route("v{version:apiVersion}/orders/{orderId}/notes")]
[Authorize]
[EnableRateLimiting("general")]
public class OrderNotesController : ControllerBase
{
    private readonly IOrderNoteService _noteService;
    private readonly IValidator<CreateOrderNoteRequest> _validator;
    private readonly ILogger<OrderNotesController> _logger;

    public OrderNotesController(
        IOrderNoteService noteService,
        IValidator<CreateOrderNoteRequest> validator,
        ILogger<OrderNotesController> logger)
    {
        _noteService = noteService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderNote(
        string orderId,
        [FromBody] CreateOrderNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        try
        {
            var createdBy = "system"; // TODO: Get from user context
            var note = await _noteService.CreateOrderNoteAsync(orderId, request, createdBy, cancellationToken);
            return CreatedAtRoute(new { orderId }, note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

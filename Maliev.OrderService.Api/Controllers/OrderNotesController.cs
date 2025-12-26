using Maliev.OrderService.Api.Authorization;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Asp.Versioning;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Maliev.OrderService.Api.Extensions;

namespace Maliev.OrderService.Api.Controllers;

/// <summary>
/// Controller for managing order notes and comments
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("order/v{version:apiVersion}/orders/{orderId}/notes")]
[Produces("application/json")]
public class OrderNotesController : ControllerBase
{
    private readonly IOrderNoteService _noteService;
    private readonly ILogger<OrderNotesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderNotesController"/> class
    /// </summary>
    public OrderNotesController(
        IOrderNoteService noteService,
        ILogger<OrderNotesController> logger)
    {
        _noteService = noteService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new note for an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="request">The note creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created note or error if order not found</returns>
    [HttpPost]
    [RequirePermission(OrderPermissions.OrdersUpdate)]
    public async Task<IActionResult> CreateOrderNote(
        string orderId,
        [FromBody] CreateOrderNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var createdBy = User.GetUserId();
            var note = await _noteService.CreateOrderNoteAsync(orderId, request, createdBy, cancellationToken);
            return CreatedAtRoute(new { orderId }, note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

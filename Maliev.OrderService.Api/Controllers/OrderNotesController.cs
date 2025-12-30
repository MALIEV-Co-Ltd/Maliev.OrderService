using Maliev.OrderService.Api.Authorization;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Asp.Versioning;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.Services.Business;
using Microsoft.AspNetCore.Mvc;
using Maliev.OrderService.Api.Extensions;
using Maliev.OrderService.Api.DTOs.Response;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing order notes and comments
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="OrderNotesController"/> class
    /// </remarks>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("order/v{version:apiVersion}/orders/{orderId}/notes")]
    [Produces("application/json")]
    public class OrderNotesController(
        IOrderNoteService noteService,
        ILogger<OrderNotesController> logger) : ControllerBase
    {
        private readonly IOrderNoteService _noteService = noteService;
        private readonly ILogger<OrderNotesController> _logger = logger;

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
                string createdBy = User.GetUserId();
                OrderNoteResponse note = await _noteService.CreateOrderNoteAsync(orderId, request, createdBy, cancellationToken);
                return CreatedAtRoute(new { orderId }, note);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Api.Services;
using Asp.Versioning;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing orders.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderServiceService _orderServiceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersController"/> class.
        /// </summary>
        /// <param name="orderServiceService">The order service.</param>
        public OrdersController(IOrderServiceService orderServiceService)
        {
            _orderServiceService = orderServiceService;
        }

        /// <summary>
        /// Gets all orders.
        /// </summary>
        /// <returns>A list of orders.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
        {
            var orders = await _orderServiceService.GetAllOrdersAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Gets an order by its ID.
        /// </summary>
        /// <param name="id">The order ID.</param>
        /// <returns>The order with the specified ID, or NotFound if not found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id)
        {
            var order = await _orderServiceService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }

        /// <summary>
        /// Creates a new order.
        /// </summary>
        /// <param name="request">The request to create an order.</param>
        /// <returns>The newly created order.</returns>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
        {
            var order = await _orderServiceService.CreateOrderAsync(request);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }

        /// <summary>
        /// Updates an existing order.
        /// </summary>
        /// <param name="id">The ID of the order to update.</param>
        /// <param name="request">The request to update an order.</param>
        /// <returns>The updated order, or NotFound if not found.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<OrderDto>> UpdateOrder(int id, UpdateOrderRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var order = await _orderServiceService.UpdateOrderAsync(request);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }

        /// <summary>
        /// Deletes an order by its ID.
        /// </summary>
        /// <param name="id">The order ID.</param>
        /// <returns>NoContent if successful, or NotFound if not found.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var result = await _orderServiceService.DeleteOrderAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
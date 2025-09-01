using Microsoft.AspNetCore.Mvc;
using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Api.Services;
using Asp.Versioning;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing order files.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/orderfiles")]
    public class OrderFilesController : ControllerBase
    {
        private readonly IOrderServiceService _orderServiceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFilesController"/> class.
        /// </summary>
        /// <param name="orderServiceService">The order service.</param>
        public OrderFilesController(IOrderServiceService orderServiceService)
        {
            _orderServiceService = orderServiceService;
        }

        /// <summary>
        /// Gets all order files.
        /// </summary>
        /// <returns>A list of order files.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderFileDto>>> GetAllOrderFiles()
        {
            var orderFiles = await _orderServiceService.GetAllOrderFilesAsync();
            return Ok(orderFiles);
        }

        /// <summary>
        /// Gets an order file by its ID.
        /// </summary>
        /// <param name="id">The order file ID.</param>
        /// <returns>The order file with the specified ID, or NotFound if not found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderFileDto>> GetOrderFileById(int id)
        {
            var orderFile = await _orderServiceService.GetOrderFileByIdAsync(id);
            if (orderFile == null)
            {
                return NotFound();
            }
            return Ok(orderFile);
        }

        /// <summary>
        /// Creates a new order file.
        /// </summary>
        /// <param name="request">The request to create an order file.</param>
        /// <returns>The newly created order file.</returns>
        [HttpPost]
        public async Task<ActionResult<OrderFileDto>> CreateOrderFile(CreateOrderFileRequest request)
        {
            var orderFile = await _orderServiceService.CreateOrderFileAsync(request);
            return CreatedAtAction(nameof(GetOrderFileById), new { id = orderFile.Id }, orderFile);
        }

        /// <summary>
        /// Updates an existing order file.
        /// </summary>
        /// <param name="id">The ID of the order file to update.</param>
        /// <param name="request">The request to update an order file.</param>
        /// <returns>The updated order file, or NotFound if not found.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<OrderFileDto>> UpdateOrderFile(int id, UpdateOrderFileRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var orderFile = await _orderServiceService.UpdateOrderFileAsync(request);
            if (orderFile == null)
            {
                return NotFound();
            }
            return Ok(orderFile);
        }

        /// <summary>
        /// Deletes an order file by its ID.
        /// </summary>
        /// <param name="id">The order file ID.</param>
        /// <returns>NoContent if successful, or NotFound if not found.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderFile(int id)
        {
            var result = await _orderServiceService.DeleteOrderFileAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
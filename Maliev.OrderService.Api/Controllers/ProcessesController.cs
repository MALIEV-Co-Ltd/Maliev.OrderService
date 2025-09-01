using Microsoft.AspNetCore.Mvc;
using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Api.Services;
using Asp.Versioning;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing processes.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/processes")]
    public class ProcessesController : ControllerBase
    {
        private readonly IOrderServiceService _orderServiceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessesController"/> class.
        /// </summary>
        /// <param name="orderServiceService">The order service.</param>
        public ProcessesController(IOrderServiceService orderServiceService)
        {
            _orderServiceService = orderServiceService;
        }

        /// <summary>
        /// Gets all processes.
        /// </summary>
        /// <returns>A list of processes.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProcessDto>>> GetAllProcesses()
        {
            var processes = await _orderServiceService.GetAllProcessesAsync();
            return Ok(processes);
        }

        /// <summary>
        /// Gets a process by its ID.
        /// </summary>
        /// <param name="id">The process ID.</param>
        /// <returns>The process with the specified ID, or NotFound if not found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProcessDto>> GetProcessById(int id)
        {
            var process = await _orderServiceService.GetProcessByIdAsync(id);
            if (process == null)
            {
                return NotFound();
            }
            return Ok(process);
        }

        /// <summary>
        /// Creates a new process.
        /// </summary>
        /// <param name="request">The request to create a process.</param>
        /// <returns>The newly created process.</returns>
        [HttpPost]
        public async Task<ActionResult<ProcessDto>> CreateProcess(CreateProcessRequest request)
        {
            var process = await _orderServiceService.CreateProcessAsync(request);
            return CreatedAtAction(nameof(GetProcessById), new { id = process.Id }, process);
        }

        /// <summary>
        /// Updates an existing process.
        /// </summary>
        /// <param name="id">The ID of the process to update.</param>
        /// <param name="request">The request to update a process.</param>
        /// <returns>The updated process, or NotFound if not found.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ProcessDto>> UpdateProcess(int id, UpdateProcessRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var process = await _orderServiceService.UpdateProcessAsync(request);
            if (process == null)
            {
                return NotFound();
            }
            return Ok(process);
        }

        /// <summary>
        /// Deletes a process by its ID.
        /// </summary>
        /// <param name="id">The process ID.</param>
        /// <returns>NoContent if successful, or NotFound if not found.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProcess(int id)
        {
            var result = await _orderServiceService.DeleteProcessAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
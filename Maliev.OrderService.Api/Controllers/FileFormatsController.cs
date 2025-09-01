using Microsoft.AspNetCore.Mvc;
using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Api.Services;
using Asp.Versioning;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing file formats.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/fileformats")]
    public class FileFormatsController : ControllerBase
    {
        private readonly IOrderServiceService _orderServiceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileFormatsController"/> class.
        /// </summary>
        /// <param name="orderServiceService">The order service.</param>
        public FileFormatsController(IOrderServiceService orderServiceService)
        {
            _orderServiceService = orderServiceService;
        }

        /// <summary>
        /// Gets all file formats.
        /// </summary>
        /// <returns>A list of file formats.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileFormatDto>>> GetAllFileFormats()
        {
            var fileFormats = await _orderServiceService.GetAllFileFormatsAsync();
            return Ok(fileFormats);
        }

        /// <summary>
        /// Gets a file format by its ID.
        /// </summary>
        /// <param name="id">The file format ID.</param>
        /// <returns>The file format with the specified ID, or NotFound if not found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<FileFormatDto>> GetFileFormatById(int id)
        {
            var fileFormat = await _orderServiceService.GetFileFormatByIdAsync(id);
            if (fileFormat == null)
            {
                return NotFound();
            }
            return Ok(fileFormat);
        }

        /// <summary>
        /// Creates a new file format.
        /// </summary>
        /// <param name="request">The request to create a file format.</param>
        /// <returns>The newly created file format.</returns>
        [HttpPost]
        public async Task<ActionResult<FileFormatDto>> CreateFileFormat(CreateFileFormatRequest request)
        {
            var fileFormat = await _orderServiceService.CreateFileFormatAsync(request);
            return CreatedAtAction(nameof(GetFileFormatById), new { id = fileFormat.Id }, fileFormat);
        }

        /// <summary>
        /// Updates an existing file format.
        /// </summary>
        /// <param name="id">The ID of the file format to update.</param>
        /// <param name="request">The request to update a file format.</param>
        /// <returns>The updated file format, or NotFound if not found.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<FileFormatDto>> UpdateFileFormat(int id, UpdateFileFormatRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var fileFormat = await _orderServiceService.UpdateFileFormatAsync(request);
            if (fileFormat == null)
            {
                return NotFound();
            }
            return Ok(fileFormat);
        }

        /// <summary>
        /// Deletes a file format by its ID.
        /// </summary>
        /// <param name="id">The file format ID.</param>
        /// <returns>NoContent if successful, or NotFound if not found.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFileFormat(int id)
        {
            var result = await _orderServiceService.DeleteFileFormatAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
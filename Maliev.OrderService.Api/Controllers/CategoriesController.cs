using Microsoft.AspNetCore.Mvc;
using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Api.Services;
using Asp.Versioning;

namespace Maliev.OrderService.Api.Controllers
{
    /// <summary>
    /// Controller for managing categories.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly IOrderServiceService _orderServiceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoriesController"/> class.
        /// </summary>
        /// <param name="orderServiceService">The order service.</param>
        public CategoriesController(IOrderServiceService orderServiceService)
        {
            _orderServiceService = orderServiceService;
        }

        /// <summary>
        /// Gets all categories.
        /// </summary>
        /// <returns>A list of categories.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
        {
            var categories = await _orderServiceService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Gets a category by its ID.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <returns>The category with the specified ID, or NotFound if not found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategoryById(int id)
        {
            var category = await _orderServiceService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        /// <param name="request">The request to create a category.</param>
        /// <returns>The newly created category.</returns>
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryRequest request)
        {
            var category = await _orderServiceService.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
        }

        /// <summary>
        /// Updates an existing category.
        /// </summary>
        /// <param name="id">The ID of the category to update.</param>
        /// <param name="request">The request to update a category.</param>
        /// <returns>The updated category, or NotFound if not found.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, UpdateCategoryRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var category = await _orderServiceService.UpdateCategoryAsync(request);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        /// <summary>
        /// Deletes a category by its ID.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <returns>NoContent if successful, or NotFound if not found.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var result = await _orderServiceService.DeleteCategoryAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
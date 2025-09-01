using Moq;
using Xunit;
using Maliev.OrderService.Api.Controllers;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.OrderService.Tests.Categories
{
    public class CategoriesControllerTests
    {
        private readonly Mock<IOrderServiceService> _mockOrderServiceService;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            _mockOrderServiceService = new Mock<IOrderServiceService>();
            _controller = new CategoriesController(_mockOrderServiceService.Object);
        }

        [Fact]
        public async Task GetAllCategories_ReturnsOkResult_WithListOfCategories()
        {
            // Arrange
            var categories = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "TestCategory" } };
            _mockOrderServiceService.Setup(service => service.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _controller.GetAllCategories();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCategories = Assert.IsType<List<CategoryDto>>(okResult.Value);
            Assert.Single(returnedCategories);
        }

        [Fact]
        public async Task GetCategoryById_ReturnsOkResult_WhenCategoryExists()
        {
            // Arrange
            var category = new CategoryDto { Id = 1, Name = "TestCategory" };
            _mockOrderServiceService.Setup(service => service.GetCategoryByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _controller.GetCategoryById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCategory = Assert.IsType<CategoryDto>(okResult.Value);
            Assert.Equal(1, returnedCategory.Id);
        }

        [Fact]
        public async Task GetCategoryById_ReturnsNotFoundResult_WhenCategoryDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((CategoryDto?)null);

            // Act
            var result = await _controller.GetCategoryById(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateCategoryRequest { Name = "NewCategory" };
            var createdCategory = new CategoryDto { Id = 1, Name = "NewCategory" };
            _mockOrderServiceService.Setup(service => service.CreateCategoryAsync(request))
                .ReturnsAsync(createdCategory);

            // Act
            var result = await _controller.CreateCategory(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedCategory = Assert.IsType<CategoryDto>(createdAtActionResult.Value);
            Assert.Equal(createdCategory.Id, returnedCategory.Id);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsOkResult_WhenCategoryExists()
        {
            // Arrange
            var request = new UpdateCategoryRequest { Id = 1, Name = "UpdatedCategory" };
            var updatedCategory = new CategoryDto { Id = 1, Name = "UpdatedCategory" };
            _mockOrderServiceService.Setup(service => service.UpdateCategoryAsync(request))
                .ReturnsAsync(updatedCategory);

            // Act
            var result = await _controller.UpdateCategory(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCategory = Assert.IsType<CategoryDto>(okResult.Value);
            Assert.Equal(updatedCategory.Id, returnedCategory.Id);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var request = new UpdateCategoryRequest { Id = 1, Name = "UpdatedCategory" };

            // Act
            var result = await _controller.UpdateCategory(2, request);

            // Assert
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            var request = new UpdateCategoryRequest { Id = 1, Name = "UpdatedCategory" };
            _mockOrderServiceService.Setup(service => service.UpdateCategoryAsync(request))
                .ReturnsAsync((CategoryDto?)null);

            // Act
            var result = await _controller.UpdateCategory(1, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNoContent_WhenCategoryExists()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteCategoryAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteCategory(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNotFound_WhenCategoryDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteCategoryAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteCategory(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

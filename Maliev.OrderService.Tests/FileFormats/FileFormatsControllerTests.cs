using Moq;
using Xunit;
using Maliev.OrderService.Api.Controllers;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.OrderService.Tests.FileFormats
{
    public class FileFormatsControllerTests
    {
        private readonly Mock<IOrderServiceService> _mockOrderServiceService;
        private readonly FileFormatsController _controller;

        public FileFormatsControllerTests()
        {
            _mockOrderServiceService = new Mock<IOrderServiceService>();
            _controller = new FileFormatsController(_mockOrderServiceService.Object);
        }

        [Fact]
        public async Task GetAllFileFormats_ReturnsOkResult_WithListOfFileFormats()
        {
            // Arrange
            var fileFormats = new List<FileFormatDto> { new FileFormatDto { Id = 1, Name = "TestFormat", Extension = ".txt" } };
            _mockOrderServiceService.Setup(service => service.GetAllFileFormatsAsync())
                .ReturnsAsync(fileFormats);

            // Act
            var result = await _controller.GetAllFileFormats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFileFormats = Assert.IsType<List<FileFormatDto>>(okResult.Value);
            Assert.Single(returnedFileFormats);
        }

        [Fact]
        public async Task GetFileFormatById_ReturnsOkResult_WhenFileFormatExists()
        {
            // Arrange
            var fileFormat = new FileFormatDto { Id = 1, Name = "TestFormat", Extension = ".txt" };
            _mockOrderServiceService.Setup(service => service.GetFileFormatByIdAsync(1))
                .ReturnsAsync(fileFormat);

            // Act
            var result = await _controller.GetFileFormatById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFileFormat = Assert.IsType<FileFormatDto>(okResult.Value);
            Assert.Equal(1, returnedFileFormat.Id);
        }

        [Fact]
        public async Task GetFileFormatById_ReturnsNotFoundResult_WhenFileFormatDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.GetFileFormatByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((FileFormatDto?)null);

            // Act
            var result = await _controller.GetFileFormatById(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateFileFormat_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateFileFormatRequest { Name = "NewFormat", Extension = ".pdf" };
            var createdFileFormat = new FileFormatDto { Id = 1, Name = "NewFormat", Extension = ".pdf" };
            _mockOrderServiceService.Setup(service => service.CreateFileFormatAsync(request))
                .ReturnsAsync(createdFileFormat);

            // Act
            var result = await _controller.CreateFileFormat(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedFileFormat = Assert.IsType<FileFormatDto>(createdAtActionResult.Value);
            Assert.Equal(createdFileFormat.Id, returnedFileFormat.Id);
        }

        [Fact]
        public async Task UpdateFileFormat_ReturnsOkResult_WhenFileFormatExists()
        {
            // Arrange
            var request = new UpdateFileFormatRequest { Id = 1, Name = "UpdatedFormat", Extension = ".docx" };
            var updatedFileFormat = new FileFormatDto { Id = 1, Name = "UpdatedFormat", Extension = ".docx" };
            _mockOrderServiceService.Setup(service => service.UpdateFileFormatAsync(request))
                .ReturnsAsync(updatedFileFormat);

            // Act
            var result = await _controller.UpdateFileFormat(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFileFormat = Assert.IsType<FileFormatDto>(okResult.Value);
            Assert.Equal(updatedFileFormat.Id, returnedFileFormat.Id);
        }

        [Fact]
        public async Task UpdateFileFormat_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var request = new UpdateFileFormatRequest { Id = 1, Name = "UpdatedFormat", Extension = ".docx" };

            // Act
            var result = await _controller.UpdateFileFormat(2, request);

            // Assert
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UpdateFileFormat_ReturnsNotFound_WhenFileFormatDoesNotExist()
        {
            // Arrange
            var request = new UpdateFileFormatRequest { Id = 1, Name = "UpdatedFormat", Extension = ".docx" };
            _mockOrderServiceService.Setup(service => service.UpdateFileFormatAsync(request))
                .ReturnsAsync((FileFormatDto?)null);

            // Act
            var result = await _controller.UpdateFileFormat(1, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteFileFormat_ReturnsNoContent_WhenFileFormatExists()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteFileFormatAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFileFormat(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteFileFormat_ReturnsNotFound_WhenFileFormatDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteFileFormatAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteFileFormat(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

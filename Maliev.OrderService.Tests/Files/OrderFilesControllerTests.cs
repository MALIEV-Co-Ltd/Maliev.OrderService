using Moq;
using Xunit;
using Maliev.OrderService.Api.Controllers;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.OrderService.Tests.OrderFiles
{
    public class OrderFilesControllerTests
    {
        private readonly Mock<IOrderServiceService> _mockOrderServiceService;
        private readonly OrderFilesController _controller;

        public OrderFilesControllerTests()
        {
            _mockOrderServiceService = new Mock<IOrderServiceService>();
            _controller = new OrderFilesController(_mockOrderServiceService.Object);
        }

        [Fact]
        public async Task GetAllOrderFiles_ReturnsOkResult_WithListOfOrderFiles()
        {
            // Arrange
            var orderFiles = new List<OrderFileDto> { new OrderFileDto { Id = 1, Bucket = "TestBucket", ObjectName = "TestObject", OrderId = 1 } };
            _mockOrderServiceService.Setup(service => service.GetAllOrderFilesAsync())
                .ReturnsAsync(orderFiles);

            // Act
            var result = await _controller.GetAllOrderFiles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrderFiles = Assert.IsType<List<OrderFileDto>>(okResult.Value);
            Assert.Single(returnedOrderFiles);
        }

        [Fact]
        public async Task GetOrderFileById_ReturnsOkResult_WhenOrderFileExists()
        {
            // Arrange
            var orderFile = new OrderFileDto { Id = 1, Bucket = "TestBucket", ObjectName = "TestObject", OrderId = 1 };
            _mockOrderServiceService.Setup(service => service.GetOrderFileByIdAsync(1))
                .ReturnsAsync(orderFile);

            // Act
            var result = await _controller.GetOrderFileById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrderFile = Assert.IsType<OrderFileDto>(okResult.Value);
            Assert.Equal(1, returnedOrderFile.Id);
        }

        [Fact]
        public async Task GetOrderFileById_ReturnsNotFoundResult_WhenOrderFileDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.GetOrderFileByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((OrderFileDto?)null);

            // Act
            var result = await _controller.GetOrderFileById(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateOrderFile_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateOrderFileRequest { Bucket = "NewBucket", ObjectName = "NewObject", OrderId = 1 };
            var createdOrderFile = new OrderFileDto { Id = 1, Bucket = "NewBucket", ObjectName = "NewObject", OrderId = 1 };
            _mockOrderServiceService.Setup(service => service.CreateOrderFileAsync(request))
                .ReturnsAsync(createdOrderFile);

            // Act
            var result = await _controller.CreateOrderFile(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedOrderFile = Assert.IsType<OrderFileDto>(createdAtActionResult.Value);
            Assert.Equal(createdOrderFile.Id, returnedOrderFile.Id);
        }

        [Fact]
        public async Task UpdateOrderFile_ReturnsOkResult_WhenOrderFileExists()
        {
            // Arrange
            var request = new UpdateOrderFileRequest { Id = 1, Bucket = "UpdatedBucket", ObjectName = "UpdatedObject", OrderId = 1 };
            var updatedOrderFile = new OrderFileDto { Id = 1, Bucket = "UpdatedBucket", ObjectName = "UpdatedObject", OrderId = 1 };
            _mockOrderServiceService.Setup(service => service.UpdateOrderFileAsync(request))
                .ReturnsAsync(updatedOrderFile);

            // Act
            var result = await _controller.UpdateOrderFile(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrderFile = Assert.IsType<OrderFileDto>(okResult.Value);
            Assert.Equal(updatedOrderFile.Id, returnedOrderFile.Id);
        }

        [Fact]
        public async Task UpdateOrderFile_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var request = new UpdateOrderFileRequest { Id = 1, Bucket = "UpdatedBucket", ObjectName = "UpdatedObject", OrderId = 1 };

            // Act
            var result = await _controller.UpdateOrderFile(2, request);

            // Assert
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UpdateOrderFile_ReturnsNotFound_WhenOrderFileDoesNotExist()
        {
            // Arrange
            var request = new UpdateOrderFileRequest { Id = 1, Bucket = "UpdatedBucket", ObjectName = "UpdatedObject", OrderId = 1 };
            _mockOrderServiceService.Setup(service => service.UpdateOrderFileAsync(request))
                .ReturnsAsync((OrderFileDto?)null);

            // Act
            var result = await _controller.UpdateOrderFile(1, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteOrderFile_ReturnsNoContent_WhenOrderFileExists()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteOrderFileAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteOrderFile(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteOrderFile_ReturnsNotFound_WhenOrderFileDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteOrderFileAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteOrderFile(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

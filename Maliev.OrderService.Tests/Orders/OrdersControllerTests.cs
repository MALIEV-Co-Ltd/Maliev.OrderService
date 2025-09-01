using Moq;
using Xunit;
using Maliev.OrderService.Api.Controllers;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.OrderService.Tests.Orders
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderServiceService> _mockOrderServiceService;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mockOrderServiceService = new Mock<IOrderServiceService>();
            _controller = new OrdersController(_mockOrderServiceService.Object);
        }

        [Fact]
        public async Task GetAllOrders_ReturnsOkResult_WithListOfOrders()
        {
            // Arrange
            var orders = new List<OrderDto> { new OrderDto { Id = 1, Name = "TestOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 } };
            _mockOrderServiceService.Setup(service => service.GetAllOrdersAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.GetAllOrders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrders = Assert.IsType<List<OrderDto>>(okResult.Value);
            Assert.Single(returnedOrders);
        }

        [Fact]
        public async Task GetOrderById_ReturnsOkResult_WhenOrderExists()
        {
            // Arrange
            var order = new OrderDto { Id = 1, Name = "TestOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };
            _mockOrderServiceService.Setup(service => service.GetOrderByIdAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _controller.GetOrderById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrder = Assert.IsType<OrderDto>(okResult.Value);
            Assert.Equal(1, returnedOrder.Id);
        }

        [Fact]
        public async Task GetOrderById_ReturnsNotFoundResult_WhenOrderDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((OrderDto?)null);

            // Act
            var result = await _controller.GetOrderById(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateOrder_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateOrderRequest { Name = "NewOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };
            var createdOrder = new OrderDto { Id = 1, Name = "NewOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };
            _mockOrderServiceService.Setup(service => service.CreateOrderAsync(request))
                .ReturnsAsync(createdOrder);

            // Act
            var result = await _controller.CreateOrder(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedOrder = Assert.IsType<OrderDto>(createdAtActionResult.Value);
            Assert.Equal(createdOrder.Id, returnedOrder.Id);
        }

        [Fact]
        public async Task UpdateOrder_ReturnsOkResult_WhenOrderExists()
        {
            // Arrange
            var request = new UpdateOrderRequest { Id = 1, Name = "UpdatedOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };
            var updatedOrder = new OrderDto { Id = 1, Name = "UpdatedOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };
            _mockOrderServiceService.Setup(service => service.UpdateOrderAsync(request))
                .ReturnsAsync(updatedOrder);

            // Act
            var result = await _controller.UpdateOrder(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrder = Assert.IsType<OrderDto>(okResult.Value);
            Assert.Equal(updatedOrder.Id, returnedOrder.Id);
        }

        [Fact]
        public async Task UpdateOrder_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var request = new UpdateOrderRequest { Id = 1, Name = "UpdatedOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };

            // Act
            var result = await _controller.UpdateOrder(2, request);

            // Assert
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UpdateOrder_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var request = new UpdateOrderRequest { Id = 1, Name = "UpdatedOrder", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };
            _mockOrderServiceService.Setup(service => service.UpdateOrderAsync(request))
                .ReturnsAsync((OrderDto?)null);

            // Act
            var result = await _controller.UpdateOrder(1, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteOrder_ReturnsNoContent_WhenOrderExists()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteOrderAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteOrder(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteOrder_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteOrderAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteOrder(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

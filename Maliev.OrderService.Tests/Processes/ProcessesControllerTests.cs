using Moq;
using Xunit;
using Maliev.OrderService.Api.Controllers;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.OrderService.Tests.Processes
{
    public class ProcessesControllerTests
    {
        private readonly Mock<IOrderServiceService> _mockOrderServiceService;
        private readonly ProcessesController _controller;

        public ProcessesControllerTests()
        {
            _mockOrderServiceService = new Mock<IOrderServiceService>();
            _controller = new ProcessesController(_mockOrderServiceService.Object);
        }

        [Fact]
        public async Task GetAllProcesses_ReturnsOkResult_WithListOfProcesses()
        {
            // Arrange
            var processes = new List<ProcessDto> { new ProcessDto { Id = 1, Name = "TestProcess", CategoryId = 1 } };
            _mockOrderServiceService.Setup(service => service.GetAllProcessesAsync())
                .ReturnsAsync(processes);

            // Act
            var result = await _controller.GetAllProcesses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedProcesses = Assert.IsType<List<ProcessDto>>(okResult.Value);
            Assert.Single(returnedProcesses);
        }

        [Fact]
        public async Task GetProcessById_ReturnsOkResult_WhenProcessExists()
        {
            // Arrange
            var process = new ProcessDto { Id = 1, Name = "TestProcess", CategoryId = 1 };
            _mockOrderServiceService.Setup(service => service.GetProcessByIdAsync(1))
                .ReturnsAsync(process);

            // Act
            var result = await _controller.GetProcessById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedProcess = Assert.IsType<ProcessDto>(okResult.Value);
            Assert.Equal(1, returnedProcess.Id);
        }

        [Fact]
        public async Task GetProcessById_ReturnsNotFoundResult_WhenProcessDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.GetProcessByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ProcessDto?)null);

            // Act
            var result = await _controller.GetProcessById(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateProcess_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateProcessRequest { Name = "NewProcess", CategoryId = 1 };
            var createdProcess = new ProcessDto { Id = 1, Name = "NewProcess", CategoryId = 1 };
            _mockOrderServiceService.Setup(service => service.CreateProcessAsync(request))
                .ReturnsAsync(createdProcess);

            // Act
            var result = await _controller.CreateProcess(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedProcess = Assert.IsType<ProcessDto>(createdAtActionResult.Value);
            Assert.Equal(createdProcess.Id, returnedProcess.Id);
        }

        [Fact]
        public async Task UpdateProcess_ReturnsOkResult_WhenProcessExists()
        {
            // Arrange
            var request = new UpdateProcessRequest { Id = 1, Name = "UpdatedProcess", CategoryId = 1 };
            var updatedProcess = new ProcessDto { Id = 1, Name = "UpdatedProcess", CategoryId = 1 };
            _mockOrderServiceService.Setup(service => service.UpdateProcessAsync(request))
                .ReturnsAsync(updatedProcess);

            // Act
            var result = await _controller.UpdateProcess(1, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedProcess = Assert.IsType<ProcessDto>(okResult.Value);
            Assert.Equal(updatedProcess.Id, returnedProcess.Id);
        }

        [Fact]
        public async Task UpdateProcess_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var request = new UpdateProcessRequest { Id = 1, Name = "UpdatedProcess", CategoryId = 1 };

            // Act
            var result = await _controller.UpdateProcess(2, request);

            // Assert
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UpdateProcess_ReturnsNotFound_WhenProcessDoesNotExist()
        {
            // Arrange
            var request = new UpdateProcessRequest { Id = 1, Name = "UpdatedProcess", CategoryId = 1 };
            _mockOrderServiceService.Setup(service => service.UpdateProcessAsync(request))
                .ReturnsAsync((ProcessDto?)null);

            // Act
            var result = await _controller.UpdateProcess(1, request);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteProcess_ReturnsNoContent_WhenProcessExists()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteProcessAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteProcess(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteProcess_ReturnsNotFound_WhenProcessDoesNotExist()
        {
            // Arrange
            _mockOrderServiceService.Setup(service => service.DeleteProcessAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteProcess(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

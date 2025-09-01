using Moq;
using Xunit;
using Maliev.OrderService.Api.Services;
using Maliev.OrderService.Data.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Maliev.OrderService.Api.Models.DTOs;

namespace Maliev.OrderService.Tests.Services
{
    public class OrderServiceServiceTests
    {
        private readonly OrderContext _context;
        private readonly OrderServiceService _service;

        public OrderServiceServiceTests()
        {
            var options = new DbContextOptionsBuilder<OrderContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new OrderContext(options);
            _service = new OrderServiceService(_context);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ReturnsAllCategories()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.Category.Add(new Category { Id = 2, Name = "Category2", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsCategory_WhenCategoryExists()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetCategoryByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Category1", result.Name);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsNull_WhenCategoryDoesNotExist()
        {
            // Act
            var result = await _service.GetCategoryByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateCategoryAsync_CreatesNewCategory()
        {
            // Arrange
            var request = new CreateCategoryRequest { Name = "NewCategory" };

            // Act
            var result = await _service.CreateCategoryAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewCategory", result.Name);
            Assert.True(result.Id > 0);
            Assert.NotNull(await _context.Category.FindAsync(result.Id));
        }

        [Fact]
        public async Task UpdateCategoryAsync_UpdatesCategory_WhenCategoryExists()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            var request = new UpdateCategoryRequest { Id = 1, Name = "UpdatedCategory" };

            // Act
            var result = await _service.UpdateCategoryAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("UpdatedCategory", result.Name);
            Assert.Equal(1, result.Id);
            Assert.Equal("UpdatedCategory", (await _context.Category.FindAsync(1))?.Name);
        }

        [Fact]
        public async Task UpdateCategoryAsync_ReturnsNull_WhenCategoryDoesNotExist()
        {
            // Arrange
            var request = new UpdateCategoryRequest { Id = 99, Name = "NonExistentCategory" };

            // Act
            var result = await _service.UpdateCategoryAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteCategoryAsync_DeletesCategory_WhenCategoryExists()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteCategoryAsync(1);

            // Assert
            Assert.True(result);
            Assert.Null(await _context.Category.FindAsync(1));
        }

        [Fact]
        public async Task DeleteCategoryAsync_ReturnsFalse_WhenCategoryDoesNotExist()
        {
            // Act
            var result = await _service.DeleteCategoryAsync(99);

            // Assert
            Assert.False(result);
        }

        // FileFormats
        [Fact]
        public async Task GetAllFileFormatsAsync_ReturnsAllFileFormats()
        {
            // Arrange
            _context.FileFormat.Add(new FileFormat { Id = 1, Name = "Format1", Extension = ".txt", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.FileFormat.Add(new FileFormat { Id = 2, Name = "Format2", Extension = ".pdf", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllFileFormatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetFileFormatByIdAsync_ReturnsFileFormat_WhenFileFormatExists()
        {
            // Arrange
            _context.FileFormat.Add(new FileFormat { Id = 1, Name = "Format1", Extension = ".txt", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetFileFormatByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Format1", result.Name);
        }

        [Fact]
        public async Task GetFileFormatByIdAsync_ReturnsNull_WhenFileFormatDoesNotExist()
        {
            // Act
            var result = await _service.GetFileFormatByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateFileFormatAsync_CreatesNewFileFormat()
        {
            // Arrange
            var request = new CreateFileFormatRequest { Name = "NewFormat", Extension = ".docx" };

            // Act
            var result = await _service.CreateFileFormatAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewFormat", result.Name);
            Assert.Equal(".docx", result.Extension);
            Assert.True(result.Id > 0);
            Assert.NotNull(await _context.FileFormat.FindAsync(result.Id));
        }

        [Fact]
        public async Task UpdateFileFormatAsync_UpdatesFileFormat_WhenFileFormatExists()
        {
            // Arrange
            _context.FileFormat.Add(new FileFormat { Id = 1, Name = "Format1", Extension = ".txt", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            var request = new UpdateFileFormatRequest { Id = 1, Name = "UpdatedFormat", Extension = ".pdf" };

            // Act
            var result = await _service.UpdateFileFormatAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("UpdatedFormat", result.Name);
            Assert.Equal(".pdf", result.Extension);
            Assert.Equal(1, result.Id);
            Assert.Equal("UpdatedFormat", (await _context.FileFormat.FindAsync(1))?.Name);
        }

        [Fact]
        public async Task UpdateFileFormatAsync_ReturnsNull_WhenFileFormatDoesNotExist()
        {
            // Arrange
            var request = new UpdateFileFormatRequest { Id = 99, Name = "NonExistentFormat", Extension = ".xyz" };

            // Act
            var result = await _service.UpdateFileFormatAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteFileFormatAsync_DeletesFileFormat_WhenFileFormatExists()
        {
            // Arrange
            _context.FileFormat.Add(new FileFormat { Id = 1, Name = "Format1", Extension = ".txt", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteFileFormatAsync(1);

            // Assert
            Assert.True(result);
            Assert.Null(await _context.FileFormat.FindAsync(1));
        }

        [Fact]
        public async Task DeleteFileFormatAsync_ReturnsFalse_WhenFileFormatDoesNotExist()
        {
            // Act
            var result = await _service.DeleteFileFormatAsync(99);

            // Assert
            Assert.False(result);
        }

        // Orders
        [Fact]
        public async Task GetAllOrdersAsync_ReturnsAllOrders()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.Order.Add(new Order { Id = 2, Name = "Order2", Quantity = 2, UnitPrice = 20, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllOrdersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetOrderByIdAsync_ReturnsOrder_WhenOrderExists()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetOrderByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Order1", result.Name);
        }

        [Fact]
        public async Task GetOrderByIdAsync_ReturnsNull_WhenOrderDoesNotExist()
        {
            // Act
            var result = await _service.GetOrderByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrderAsync_CreatesNewOrder()
        {
            // Arrange
            var request = new CreateOrderRequest { Name = "NewOrder", Quantity = 5, UnitPrice = 50, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };

            // Act
            var result = await _service.CreateOrderAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewOrder", result.Name);
            Assert.True(result.Id > 0);
            Assert.NotNull(await _context.Order.FindAsync(result.Id));
        }

        [Fact]
        public async Task UpdateOrderAsync_UpdatesOrder_WhenOrderExists()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            var request = new UpdateOrderRequest { Id = 1, Name = "UpdatedOrder", Quantity = 2, UnitPrice = 15, DiscountPercent = 5, Manufactured = 1, ProcessId = 1 };

            // Act
            var result = await _service.UpdateOrderAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("UpdatedOrder", result.Name);
            Assert.Equal(1, result.Id);
            Assert.Equal("UpdatedOrder", (await _context.Order.FindAsync(1))?.Name);
        }

        [Fact]
        public async Task UpdateOrderAsync_ReturnsNull_WhenOrderDoesNotExist()
        {
            // Arrange
            var request = new UpdateOrderRequest { Id = 99, Name = "NonExistentOrder", Quantity = 1, UnitPrice = 1, DiscountPercent = 0, Manufactured = 0, ProcessId = 1 };

            // Act
            var result = await _service.UpdateOrderAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteOrderAsync_DeletesOrder_WhenOrderExists()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteOrderAsync(1);

            // Assert
            Assert.True(result);
            Assert.Null(await _context.Order.FindAsync(1));
        }

        [Fact]
        public async Task DeleteOrderAsync_ReturnsFalse_WhenOrderDoesNotExist()
        {
            // Act
            var result = await _service.DeleteOrderAsync(99);

            // Assert
            Assert.False(result);
        }

        // OrderFiles
        [Fact]
        public async Task GetAllOrderFilesAsync_ReturnsAllOrderFiles()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.OrderFile.Add(new OrderFile { Id = 1, Bucket = "bucket1", ObjectName = "object1", OrderId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.OrderFile.Add(new OrderFile { Id = 2, Bucket = "bucket2", ObjectName = "object2", OrderId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllOrderFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetOrderFileByIdAsync_ReturnsOrderFile_WhenOrderFileExists()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.OrderFile.Add(new OrderFile { Id = 1, Bucket = "bucket1", ObjectName = "object1", OrderId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetOrderFileByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("bucket1", result.Bucket);
        }

        [Fact]
        public async Task GetOrderFileByIdAsync_ReturnsNull_WhenOrderFileDoesNotExist()
        {
            // Act
            var result = await _service.GetOrderFileByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrderFileAsync_CreatesNewOrderFile()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            var request = new CreateOrderFileRequest { Bucket = "newbucket", ObjectName = "newobject", OrderId = 1 };

            // Act
            var result = await _service.CreateOrderFileAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newbucket", result.Bucket);
            Assert.True(result.Id > 0);
            Assert.NotNull(await _context.OrderFile.FindAsync(result.Id));
        }

        [Fact]
        public async Task UpdateOrderFileAsync_UpdatesOrderFile_WhenOrderFileExists()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.OrderFile.Add(new OrderFile { Id = 1, Bucket = "bucket1", ObjectName = "object1", OrderId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            var request = new UpdateOrderFileRequest { Id = 1, Bucket = "updatedbucket", ObjectName = "updatedobject", OrderId = 1 };

            // Act
            var result = await _service.UpdateOrderFileAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("updatedbucket", result.Bucket);
            Assert.Equal(1, result.Id);
            Assert.Equal("updatedbucket", (await _context.OrderFile.FindAsync(1))?.Bucket);
        }

        [Fact]
        public async Task UpdateOrderFileAsync_ReturnsNull_WhenOrderFileDoesNotExist()
        {
            // Arrange
            var request = new UpdateOrderFileRequest { Id = 99, Bucket = "nonexistent", ObjectName = "nonexistent", OrderId = 1 };

            // Act
            var result = await _service.UpdateOrderFileAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteOrderFileAsync_DeletesOrderFile_WhenOrderFileExists()
        {
            // Arrange
            _context.Order.Add(new Order { Id = 1, Name = "Order1", Quantity = 1, UnitPrice = 10, DiscountPercent = 0, Manufactured = 0, ProcessId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.OrderFile.Add(new OrderFile { Id = 1, Bucket = "bucket1", ObjectName = "object1", OrderId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteOrderFileAsync(1);

            // Assert
            Assert.True(result);
            Assert.Null(await _context.OrderFile.FindAsync(1));
        }

        [Fact]
        public async Task DeleteOrderFileAsync_ReturnsFalse_WhenOrderFileDoesNotExist()
        {
            // Act
            var result = await _service.DeleteOrderFileAsync(99);

            // Assert
            Assert.False(result);
        }

        // Processes
        [Fact]
        public async Task GetAllProcessesAsync_ReturnsAllProcesses()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.Process.Add(new Process { Id = 1, Name = "Process1", CategoryId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.Process.Add(new Process { Id = 2, Name = "Process2", CategoryId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllProcessesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetProcessByIdAsync_ReturnsProcess_WhenProcessExists()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.Process.Add(new Process { Id = 1, Name = "Process1", CategoryId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetProcessByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Process1", result.Name);
        }

        [Fact]
        public async Task GetProcessByIdAsync_ReturnsNull_WhenProcessDoesNotExist()
        {
            // Act
            var result = await _service.GetProcessByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateProcessAsync_CreatesNewProcess()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            var request = new CreateProcessRequest { Name = "NewProcess", CategoryId = 1 };

            // Act
            var result = await _service.CreateProcessAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewProcess", result.Name);
            Assert.True(result.Id > 0);
            Assert.NotNull(await _context.Process.FindAsync(result.Id));
        }

        [Fact]
        public async Task UpdateProcessAsync_UpdatesProcess_WhenProcessExists()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.Process.Add(new Process { Id = 1, Name = "Process1", CategoryId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            var request = new UpdateProcessRequest { Id = 1, Name = "UpdatedProcess", CategoryId = 1 };

            // Act
            var result = await _service.UpdateProcessAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("UpdatedProcess", result.Name);
            Assert.Equal(1, result.Id);
            Assert.Equal("UpdatedProcess", (await _context.Process.FindAsync(1))?.Name);
        }

        [Fact]
        public async Task UpdateProcessAsync_ReturnsNull_WhenProcessDoesNotExist()
        {
            // Arrange
            var request = new UpdateProcessRequest { Id = 99, Name = "NonExistentProcess", CategoryId = 1 };

            // Act
            var result = await _service.UpdateProcessAsync(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteProcessAsync_DeletesProcess_WhenProcessExists()
        {
            // Arrange
            _context.Category.Add(new Category { Id = 1, Name = "Category1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            _context.Process.Add(new Process { Id = 1, Name = "Process1", CategoryId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteProcessAsync(1);

            // Assert
            Assert.True(result);
            Assert.Null(await _context.Process.FindAsync(1));
        }

        [Fact]
        public async Task DeleteProcessAsync_ReturnsFalse_WhenProcessDoesNotExist()
        {
            // Act
            var result = await _service.DeleteProcessAsync(99);

            // Assert
            Assert.False(result);
        }
    }
}

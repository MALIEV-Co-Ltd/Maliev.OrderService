using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Api.Services.Business;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Moq;

namespace Maliev.OrderService.Tests.Unit.Services;

public class OrderManagementServiceTests : IDisposable
{
    private readonly OrderDbContext _context;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<OrderManagementService>> _loggerMock;
    private readonly OrderManagementService _service;

    public OrderManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<OrderManagementService>>();

        _service = new OrderManagementService(
            _context,
            _mapperMock.Object,
            _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.ServiceCategories.AddRange(
            new ServiceCategory { CategoryId = 1, Name = "3D Printing", IsActive = true },
            new ServiceCategory { CategoryId = 2, Name = "CNC Machining", IsActive = true }
        );

        _context.ProcessTypes.AddRange(
            new ProcessType { ProcessTypeId = 1, Name = "FDM", ServiceCategoryId = 1, IsActive = true },
            new ProcessType { ProcessTypeId = 2, Name = "SLA", ServiceCategoryId = 1, IsActive = true }
        );

        _context.SaveChanges();
    }

    private static OrderResponse CreateTestOrderResponse(string orderId = "ORD-2025-00001")
    {
        return new OrderResponse
        {
            OrderId = orderId,
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            Version = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsOrderResponse()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            ProcessTypeId = 1,
            Requirements = "Test order"
        };

        _mapperMock.Setup(m => m.Map<Order>(request))
            .Returns(new Order
            {
                CustomerId = request.CustomerId,
                CustomerType = request.CustomerType,
                ServiceCategoryId = request.ServiceCategoryId,
                Version = new byte[8]
            });
        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns(CreateTestOrderResponse());

        // Act
        var result = await _service.CreateOrderAsync(request, "test-user");

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().StartWith("ORD-2025-");
        _context.Orders.Should().HaveCount(1);
        _context.OrderStatuses.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrder_GeneratesSequentialOrderIds()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1
        };

        _mapperMock.Setup(m => m.Map<Order>(It.IsAny<CreateOrderRequest>()))
            .Returns((CreateOrderRequest req) => new Order
            {
                CustomerId = req.CustomerId,
                CustomerType = req.CustomerType,
                ServiceCategoryId = req.ServiceCategoryId,
                Version = new byte[8]
            });
        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns((Order o) => CreateTestOrderResponse(o.OrderId));

        // Act
        var result1 = await _service.CreateOrderAsync(request, "user1");
        var result2 = await _service.CreateOrderAsync(request, "user1");

        // Assert
        result1.OrderId.Should().Be("ORD-2025-00001");
        result2.OrderId.Should().Be("ORD-2025-00002");
    }

    [Fact]
    public async Task CreateOrder_CreatesInitialStatusAsNew()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1
        };

        _mapperMock.Setup(m => m.Map<Order>(request))
            .Returns(new Order
            {
                CustomerId = request.CustomerId,
                CustomerType = request.CustomerType,
                ServiceCategoryId = request.ServiceCategoryId,
                Version = new byte[8]
            });
        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns(CreateTestOrderResponse());

        // Act
        await _service.CreateOrderAsync(request, "user1");

        // Assert
        var status = await _context.OrderStatuses.FirstOrDefaultAsync();
        status.Should().NotBeNull();
        status!.Status.Should().Be("New");
        status.UpdatedBy.Should().Be("user1");
    }

    [Fact]
    public async Task UpdateOrder_WithValidVersion_UpdatesOrder()
    {
        // Arrange
        var existingOrder = new Order
        {
            OrderId = "ORD-2025-00001",
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            Version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _context.Orders.Add(existingOrder);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateOrderRequest
        {
            Version = Convert.ToBase64String(existingOrder.Version),
            AssignedEmployeeId = "EMP-001"
        };

        _mapperMock.Setup(m => m.Map(updateRequest, existingOrder));
        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns(CreateTestOrderResponse());

        // Act
        var result = await _service.UpdateOrderAsync("ORD-2025-00001", updateRequest, "user2");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateOrder_WithStaleVersion_ThrowsConcurrencyException()
    {
        // Arrange
        var existingOrder = new Order
        {
            OrderId = "ORD-2025-00001",
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            Version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _context.Orders.Add(existingOrder);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateOrderRequest
        {
            Version = Convert.ToBase64String(new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 }),
            AssignedEmployeeId = "EMP-001"
        };

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            _service.UpdateOrderAsync("ORD-2025-00001", updateRequest, "user2"));
    }

    [Fact]
    public async Task UpdateOrder_WithNonExistentOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var updateRequest = new UpdateOrderRequest
        {
            Version = "AAAAAAAAAAAA",
            AssignedEmployeeId = "EMP-001"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateOrderAsync("ORD-INVALID", updateRequest, "user1"));
    }

    [Fact]
    public async Task CancelOrder_ExistingOrder_ReturnsTrue()
    {
        // Arrange
        var order = new Order
        {
            OrderId = "ORD-2025-00001",
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            Version = new byte[8]
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelOrderAsync("ORD-2025-00001", "user1", "Test reason");

        // Assert
        result.Should().BeTrue();
        var cancelledStatus = await _context.OrderStatuses
            .FirstOrDefaultAsync(s => s.OrderId == "ORD-2025-00001" && s.Status == "Cancelled");
        cancelledStatus.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrderByIdAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var order = new Order
        {
            OrderId = "ORD-2025-00001",
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            Version = new byte[8]
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns(CreateTestOrderResponse());

        // Act
        var result = await _service.GetOrderByIdAsync("ORD-2025-00001");

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("ORD-2025-00001");
    }

    [Fact]
    public async Task GetOrderByIdAsync_NonExistentOrder_ReturnsNull()
    {
        // Act
        var result = await _service.GetOrderByIdAsync("ORD-INVALID");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrdersAsync_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var orders = Enumerable.Range(1, 15).Select(i => new Order
        {
            OrderId = $"ORD-2025-{i:D5}",
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            Version = new byte[8]
        }).ToList();

        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns((Order o) => CreateTestOrderResponse(o.OrderId));
        _mapperMock.Setup(m => m.Map<List<OrderResponse>>(It.IsAny<List<Order>>()))
            .Returns((List<Order> orderList) => orderList.Select(o => CreateTestOrderResponse(o.OrderId)).ToList());

        // Act
        var result = await _service.GetOrdersAsync(page: 1, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task CreateOrder_InitializesVersionForInMemoryDatabase()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1
        };

        _mapperMock.Setup(m => m.Map<Order>(request))
            .Returns(new Order
            {
                CustomerId = request.CustomerId,
                CustomerType = request.CustomerType,
                ServiceCategoryId = request.ServiceCategoryId,
                Version = new byte[8]
            });
        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns(CreateTestOrderResponse());

        // Act
        await _service.CreateOrderAsync(request, "user1");

        // Assert
        var order = await _context.Orders.FirstAsync();
        order.Version.Should().NotBeNull();
        order.Version.Should().HaveCount(8);
    }

    [Fact]
    public async Task CreateOrder_SetsAuditFields()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1
        };

        _mapperMock.Setup(m => m.Map<Order>(request))
            .Returns(new Order
            {
                CustomerId = request.CustomerId,
                CustomerType = request.CustomerType,
                ServiceCategoryId = request.ServiceCategoryId,
                Version = new byte[8]
            });
        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns(CreateTestOrderResponse());

        // Act
        await _service.CreateOrderAsync(request, "test-user");

        // Assert
        var order = await _context.Orders.FirstAsync();
        order.CreatedBy.Should().Be("test-user");
        order.UpdatedBy.Should().Be("test-user");
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        order.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateOrder_UpdatesAuditFields()
    {
        // Arrange
        var existingOrder = new Order
        {
            OrderId = "ORD-2025-00001",
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _context.Orders.Add(existingOrder);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateOrderRequest
        {
            Version = Convert.ToBase64String(existingOrder.Version),
            AssignedEmployeeId = "EMP-001"
        };

        _mapperMock.Setup(m => m.Map(updateRequest, existingOrder));
        _mapperMock.Setup(m => m.Map<OrderResponse>(It.IsAny<Order>()))
            .Returns(CreateTestOrderResponse());

        // Act
        await _service.UpdateOrderAsync("ORD-2025-00001", updateRequest, "user2");

        // Assert
        var updatedOrder = await _context.Orders.FindAsync("ORD-2025-00001");
        updatedOrder!.UpdatedBy.Should().Be("user2");
        updatedOrder.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedOrder.CreatedBy.Should().Be("user1");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

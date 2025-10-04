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

public class OrderStatusServiceTests : IDisposable
{
    private readonly OrderDbContext _context;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<OrderStatusService>> _loggerMock;
    private readonly OrderStatusService _service;

    public OrderStatusServiceTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<OrderStatusService>>();

        _service = new OrderStatusService(
            _context,
            _mapperMock.Object,
            _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var testOrder = new Order
        {
            OrderId = "ORD-2025-00001",
            CustomerId = "CUST-001",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            Version = new byte[8]
        };

        _context.Orders.Add(testOrder);
        _context.SaveChanges();
    }

    private static OrderStatusResponse CreateTestOrderStatusResponse(
        long statusId = 1,
        string orderId = "ORD-2025-00001",
        string status = "New")
    {
        return new OrderStatusResponse
        {
            StatusId = statusId,
            OrderId = orderId,
            Status = status,
            UpdatedBy = "test-user",
            Timestamp = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task CreateOrderStatus_WithValidOrder_CreatesStatus()
    {
        // Arrange
        var request = new CreateOrderStatusRequest
        {
            Status = "Reviewing",
            InternalNotes = "Starting review process",
            CustomerNotes = "We are reviewing your order"
        };

        _mapperMock.Setup(m => m.Map<OrderStatus>(request))
            .Returns(new OrderStatus
            {
                Status = request.Status,
                InternalNotes = request.InternalNotes,
                CustomerNotes = request.CustomerNotes
            });
        _mapperMock.Setup(m => m.Map<OrderStatusResponse>(It.IsAny<OrderStatus>()))
            .Returns(CreateTestOrderStatusResponse(1, "ORD-2025-00001", "Reviewing"));

        // Act
        var result = await _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user1");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Reviewing");
        _context.OrderStatuses.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrderStatus_WithNonExistentOrder_ThrowsException()
    {
        // Arrange
        var request = new CreateOrderStatusRequest
        {
            Status = "Reviewing"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateOrderStatusAsync("ORD-INVALID", request, "user1"));
    }

    [Fact]
    public async Task CreateOrderStatus_WithDuplicateStatus_ThrowsException()
    {
        // Arrange
        _context.OrderStatuses.Add(new OrderStatus
        {
            OrderId = "ORD-2025-00001",
            Status = "Reviewing",
            UpdatedBy = "user1",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new CreateOrderStatusRequest
        {
            Status = "Reviewing"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user1"));
    }

    [Fact]
    public async Task CreateOrderStatus_SetsTimestampAndUpdatedBy()
    {
        // Arrange
        var request = new CreateOrderStatusRequest
        {
            Status = "Reviewing"
        };

        _mapperMock.Setup(m => m.Map<OrderStatus>(request))
            .Returns(new OrderStatus { Status = request.Status });
        _mapperMock.Setup(m => m.Map<OrderStatusResponse>(It.IsAny<OrderStatus>()))
            .Returns(CreateTestOrderStatusResponse());

        // Act
        await _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user2");

        // Assert
        var status = await _context.OrderStatuses.FirstOrDefaultAsync();
        status.Should().NotBeNull();
        status!.UpdatedBy.Should().Be("user2");
        status.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateOrderStatus_WithInternalAndCustomerNotes_SavesBoth()
    {
        // Arrange
        var request = new CreateOrderStatusRequest
        {
            Status = "Reviewing",
            InternalNotes = "Technical issues found",
            CustomerNotes = "We're reviewing your order"
        };

        _mapperMock.Setup(m => m.Map<OrderStatus>(request))
            .Returns(new OrderStatus
            {
                Status = request.Status,
                InternalNotes = request.InternalNotes,
                CustomerNotes = request.CustomerNotes
            });
        _mapperMock.Setup(m => m.Map<OrderStatusResponse>(It.IsAny<OrderStatus>()))
            .Returns(CreateTestOrderStatusResponse());

        // Act
        await _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user1");

        // Assert
        var status = await _context.OrderStatuses.FirstOrDefaultAsync();
        status.Should().NotBeNull();
        status!.InternalNotes.Should().Be("Technical issues found");
        status.CustomerNotes.Should().Be("We're reviewing your order");
    }

    [Fact]
    public async Task GetOrderStatusHistory_ReturnsOrderedHistory()
    {
        // Arrange
        var statuses = new List<OrderStatus>
        {
            new OrderStatus
            {
                OrderId = "ORD-2025-00001",
                Status = "New",
                UpdatedBy = "user1",
                Timestamp = DateTime.UtcNow.AddDays(-3)
            },
            new OrderStatus
            {
                OrderId = "ORD-2025-00001",
                Status = "Reviewing",
                UpdatedBy = "user1",
                Timestamp = DateTime.UtcNow.AddDays(-2)
            },
            new OrderStatus
            {
                OrderId = "ORD-2025-00001",
                Status = "Reviewed",
                UpdatedBy = "user1",
                Timestamp = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.OrderStatuses.AddRange(statuses);
        await _context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<List<OrderStatusResponse>>(It.IsAny<List<OrderStatus>>()))
            .Returns((List<OrderStatus> list) => list.Select((s, index) =>
                CreateTestOrderStatusResponse(index + 1, s.OrderId, s.Status)).ToList());

        // Act
        var result = await _service.GetOrderStatusHistoryAsync("ORD-2025-00001");

        // Assert
        result.Should().HaveCount(3);
        result[0].Status.Should().Be("New");
        result[1].Status.Should().Be("Reviewing");
        result[2].Status.Should().Be("Reviewed");
    }

    [Fact]
    public async Task GetOrderStatusHistory_WithNoStatuses_ReturnsEmptyList()
    {
        // Arrange
        _mapperMock.Setup(m => m.Map<List<OrderStatusResponse>>(It.IsAny<List<OrderStatus>>()))
            .Returns(new List<OrderStatusResponse>());

        // Act
        var result = await _service.GetOrderStatusHistoryAsync("ORD-2025-00001");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderStatusHistory_OnlyReturnsStatusesForSpecificOrder()
    {
        // Arrange
        var order2 = new Order
        {
            OrderId = "ORD-2025-00002",
            CustomerId = "CUST-002",
            CustomerType = "Customer",
            ServiceCategoryId = 1,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            Version = new byte[8]
        };
        _context.Orders.Add(order2);

        _context.OrderStatuses.AddRange(
            new OrderStatus
            {
                OrderId = "ORD-2025-00001",
                Status = "New",
                UpdatedBy = "user1",
                Timestamp = DateTime.UtcNow
            },
            new OrderStatus
            {
                OrderId = "ORD-2025-00002",
                Status = "New",
                UpdatedBy = "user1",
                Timestamp = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        _mapperMock.Setup(m => m.Map<List<OrderStatusResponse>>(It.IsAny<List<OrderStatus>>()))
            .Returns((List<OrderStatus> list) => list.Select((s, index) =>
                CreateTestOrderStatusResponse(index + 1, s.OrderId, s.Status)).ToList());

        // Act
        var result = await _service.GetOrderStatusHistoryAsync("ORD-2025-00001");

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderId.Should().Be("ORD-2025-00001");
    }

    [Fact]
    public async Task CreateOrderStatus_StateTransition_New_To_Reviewing()
    {
        // Arrange - Order with initial "New" status
        _context.OrderStatuses.Add(new OrderStatus
        {
            OrderId = "ORD-2025-00001",
            Status = "New",
            UpdatedBy = "user1",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });
        await _context.SaveChangesAsync();

        var request = new CreateOrderStatusRequest { Status = "Reviewing" };

        _mapperMock.Setup(m => m.Map<OrderStatus>(request))
            .Returns(new OrderStatus { Status = request.Status });
        _mapperMock.Setup(m => m.Map<OrderStatusResponse>(It.IsAny<OrderStatus>()))
            .Returns(CreateTestOrderStatusResponse(2, "ORD-2025-00001", "Reviewing"));

        // Act
        var result = await _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user1");

        // Assert
        result.Status.Should().Be("Reviewing");
        _context.OrderStatuses.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrderStatus_StateTransition_Reviewing_To_Reviewed()
    {
        // Arrange - Order with "Reviewing" status
        _context.OrderStatuses.Add(new OrderStatus
        {
            OrderId = "ORD-2025-00001",
            Status = "Reviewing",
            UpdatedBy = "user1",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });
        await _context.SaveChangesAsync();

        var request = new CreateOrderStatusRequest { Status = "Reviewed" };

        _mapperMock.Setup(m => m.Map<OrderStatus>(request))
            .Returns(new OrderStatus { Status = request.Status });
        _mapperMock.Setup(m => m.Map<OrderStatusResponse>(It.IsAny<OrderStatus>()))
            .Returns(CreateTestOrderStatusResponse(2, "ORD-2025-00001", "Reviewed"));

        // Act
        var result = await _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user1");

        // Assert
        result.Status.Should().Be("Reviewed");
        _context.OrderStatuses.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrderStatus_StateTransition_Reviewed_To_Quoted()
    {
        // Arrange - Order with "Reviewed" status
        _context.OrderStatuses.Add(new OrderStatus
        {
            OrderId = "ORD-2025-00001",
            Status = "Reviewed",
            UpdatedBy = "user1",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });
        await _context.SaveChangesAsync();

        var request = new CreateOrderStatusRequest { Status = "Quoted" };

        _mapperMock.Setup(m => m.Map<OrderStatus>(request))
            .Returns(new OrderStatus { Status = request.Status });
        _mapperMock.Setup(m => m.Map<OrderStatusResponse>(It.IsAny<OrderStatus>()))
            .Returns(CreateTestOrderStatusResponse(2, "ORD-2025-00001", "Quoted"));

        // Act
        var result = await _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user1");

        // Assert
        result.Status.Should().Be("Quoted");
        _context.OrderStatuses.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateOrderStatus_StateTransition_Any_To_Cancelled()
    {
        // Arrange - Order with "InProgress" status
        _context.OrderStatuses.Add(new OrderStatus
        {
            OrderId = "ORD-2025-00001",
            Status = "InProgress",
            UpdatedBy = "user1",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });
        await _context.SaveChangesAsync();

        var request = new CreateOrderStatusRequest
        {
            Status = "Cancelled",
            InternalNotes = "Customer requested cancellation"
        };

        _mapperMock.Setup(m => m.Map<OrderStatus>(request))
            .Returns(new OrderStatus
            {
                Status = request.Status,
                InternalNotes = request.InternalNotes
            });
        _mapperMock.Setup(m => m.Map<OrderStatusResponse>(It.IsAny<OrderStatus>()))
            .Returns(CreateTestOrderStatusResponse(2, "ORD-2025-00001", "Cancelled"));

        // Act
        var result = await _service.CreateOrderStatusAsync("ORD-2025-00001", request, "user1");

        // Assert
        result.Status.Should().Be("Cancelled");
        _context.OrderStatuses.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

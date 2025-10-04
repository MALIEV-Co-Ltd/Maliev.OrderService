using System.Globalization;
using AutoMapper;
using Maliev.OrderService.Api.DTOs.Request;
using Maliev.OrderService.Api.DTOs.Response;
using Maliev.OrderService.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services.Business;

public partial class OrderManagementService : IOrderManagementService
{
    private readonly OrderDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderManagementService> _logger;

    public OrderManagementService(OrderDbContext context, IMapper mapper, ILogger<OrderManagementService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.ServiceCategory)
            .Include(o => o.ProcessType)
            .Include(o => o.OrderStatuses)
            .Include(o => o.PrintingAttributes)
            .Include(o => o.CncAttributes)
            .Include(o => o.SheetMetalAttributes)
            .Include(o => o.ScanningAttributes)
            .Include(o => o.DesignAttributes)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        return order != null ? _mapper.Map<OrderResponse>(order) : null;
    }

    public async Task<PaginatedResponse<OrderResponse>> GetOrdersAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.ServiceCategory)
            .Include(o => o.ProcessType)
            .Include(o => o.OrderStatuses)
            .AsQueryable();

        if (!string.IsNullOrEmpty(customerId))
        {
            query = query.Where(o => o.CustomerId == customerId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<OrderResponse>
        {
            Items = _mapper.Map<List<OrderResponse>>(items),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string createdBy, CancellationToken cancellationToken = default)
    {
        // Check if there's already an active transaction (e.g., from batch operations)
        var existingTransaction = _context.Database.CurrentTransaction;
        var shouldManageTransaction = existingTransaction == null;

        // Use transaction for atomic creation of Order and initial OrderStatus (only if not in a batch)
        var transaction = shouldManageTransaction
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var order = _mapper.Map<Order>(request);
            order.OrderId = await GenerateOrderIdAsync(cancellationToken);
            order.CreatedBy = createdBy;
            order.UpdatedBy = createdBy;
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            // Note: Version (RowVersion) is database-generated, do not set manually

            _context.Orders.Add(order);

            // Create initial status
            var initialStatus = new OrderStatus
            {
                OrderId = order.OrderId,
                Status = "New",
                UpdatedBy = createdBy,
                Timestamp = DateTime.UtcNow
            };
            _context.OrderStatuses.Add(initialStatus);

            await _context.SaveChangesAsync(cancellationToken);

            // Only commit if we created the transaction
            if (shouldManageTransaction && transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return _mapper.Map<OrderResponse>(order);
        }
        catch
        {
            // Only rollback if we created the transaction (outer transaction will handle rollback otherwise)
            // Transaction automatically rolls back on exception via Dispose
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task<OrderResponse> UpdateOrderAsync(string orderId, UpdateOrderRequest request, string updatedBy, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FindAsync(new object[] { orderId }, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        // Optimistic concurrency check
        byte[] requestVersion;
        try
        {
            requestVersion = Convert.FromBase64String(request.Version);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"Invalid version format for order {orderId}. Version must be a valid Base64 string.");
        }

        if (!order.Version.SequenceEqual(requestVersion))
        {
            throw new DbUpdateConcurrencyException("Order has been modified by another user");
        }

        _mapper.Map(request, order);
        order.UpdatedBy = updatedBy;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrderResponse>(order);
    }

    public async Task<bool> CancelOrderAsync(string orderId, string cancelledBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FindAsync(new object[] { orderId }, cancellationToken);
        if (order == null)
        {
            return false;
        }

        var cancelStatus = new OrderStatus
        {
            OrderId = orderId,
            Status = "Cancelled",
            InternalNotes = reason,
            UpdatedBy = cancelledBy,
            Timestamp = DateTime.UtcNow
        };

        _context.OrderStatuses.Add(cancelStatus);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<string> GenerateOrderIdAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"ORD-{year}-";

        var lastOrder = await _context.Orders
            .Where(o => o.OrderId.StartsWith(prefix))
            .OrderByDescending(o => o.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastOrder == null)
        {
            return $"{prefix}00001";
        }

        var lastNumber = int.Parse(lastOrder.OrderId.AsSpan(prefix.Length), CultureInfo.InvariantCulture);
        return $"{prefix}{(lastNumber + 1):D5}";
    }

    private static partial class Log
    {
        // LoggerMessage delegates can be added here as needed
    }
}

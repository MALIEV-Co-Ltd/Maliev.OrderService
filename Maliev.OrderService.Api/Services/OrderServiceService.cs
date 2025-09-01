using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Data.Data;
using Maliev.OrderService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.OrderService.Api.Services
{
    /// <summary>
    /// Implements the <see cref="IOrderServiceService"/> interface, providing business logic for order-related operations.
    /// </summary>
    public class OrderServiceService : IOrderServiceService
    {
        private readonly OrderContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderServiceService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public OrderServiceService(OrderContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all categories asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="CategoryDto"/> objects.</returns>
        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            return await _context.Category
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a category by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the category.</param>
        /// <returns>The <see cref="CategoryDto"/> object if found, otherwise null.</returns>
        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            return await _context.Category
                .Where(c => c.Id == id)
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new category asynchronously.
        /// </summary>
        /// <param name="request">The request containing category data.</param>
        /// <returns>The newly created <see cref="CategoryDto"/> object.</returns>
        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
        {
            var category = new Category { Name = request.Name, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow };
            _context.Category.Add(category);
            await _context.SaveChangesAsync();
            return new CategoryDto { Id = category.Id, Name = category.Name };
        }

        /// <summary>
        /// Updates an existing category asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated category data.</param>
        /// <returns>The updated <see cref="CategoryDto"/> object if found, otherwise null.</returns>
        public async Task<CategoryDto?> UpdateCategoryAsync(UpdateCategoryRequest request)
        {
            var category = await _context.Category.FindAsync(request.Id);
            if (category == null)
            {
                return null;
            }

            category.Name = request.Name;
            category.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new CategoryDto { Id = category.Id, Name = category.Name };
        }

        /// <summary>
        /// Deletes a category by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>True if the category was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return false;
            }

            _context.Category.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Retrieves all file formats asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="FileFormatDto"/> objects.</returns>
        public async Task<IEnumerable<FileFormatDto>> GetAllFileFormatsAsync()
        {
            return await _context.FileFormat
                .Select(f => new FileFormatDto { Id = f.Id, Name = f.Name, Extension = f.Extension })
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a file format by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the file format.</param>
        /// <returns>The <see cref="FileFormatDto"/> object if found, otherwise null.</returns>
        public async Task<FileFormatDto?> GetFileFormatByIdAsync(int id)
        {
            return await _context.FileFormat
                .Where(f => f.Id == id)
                .Select(f => new FileFormatDto { Id = f.Id, Name = f.Name, Extension = f.Extension })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new file format asynchronously.
        /// </summary>
        /// <param name="request">The request containing file format data.</param>
        /// <returns>The newly created <see cref="FileFormatDto"/> object.</returns>
        public async Task<FileFormatDto> CreateFileFormatAsync(CreateFileFormatRequest request)
        {
            var fileFormat = new FileFormat { Name = request.Name, Extension = request.Extension, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow };
            _context.FileFormat.Add(fileFormat);
            await _context.SaveChangesAsync();
            return new FileFormatDto { Id = fileFormat.Id, Name = fileFormat.Name, Extension = fileFormat.Extension };
        }

        /// <summary>
        /// Updates an existing file format asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated file format data.</param>
        /// <returns>The updated <see cref="FileFormatDto"/> object if found, otherwise null.</returns>
        public async Task<FileFormatDto?> UpdateFileFormatAsync(UpdateFileFormatRequest request)
        {
            var fileFormat = await _context.FileFormat.FindAsync(request.Id);
            if (fileFormat == null)
            {
                return null;
            }

            fileFormat.Name = request.Name;
            fileFormat.Extension = request.Extension;
            fileFormat.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new FileFormatDto { Id = fileFormat.Id, Name = fileFormat.Name, Extension = fileFormat.Extension };
        }

        /// <summary>
        /// Deletes a file format by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the file format to delete.</param>
        /// <returns>True if the file format was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteFileFormatAsync(int id)
        {
            var fileFormat = await _context.FileFormat.FindAsync(id);
            if (fileFormat == null)
            {
                return false;
            }

            _context.FileFormat.Remove(fileFormat);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Retrieves all orders asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="OrderDto"/> objects.</returns>
        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            return await _context.Order
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description,
                    Quantity = o.Quantity,
                    UnitPrice = o.UnitPrice,
                    DiscountPercent = o.DiscountPercent,
                    Manufactured = o.Manufactured,
                    Subtotal = o.Subtotal,
                    Remaining = o.Remaining,
                    CreatedDate = o.CreatedDate,
                    ModifiedDate = o.ModifiedDate,
                    PromisedDate = o.PromisedDate,
                    FinishedDate = o.FinishedDate,
                    Turnaround = o.Turnaround,
                    TrackingNumber = o.TrackingNumber,
                    ProcessId = o.ProcessId,
                    ColorId = o.ColorId,
                    CurrencyId = o.CurrencyId,
                    CustomerId = o.CustomerId,
                    EmployeeId = o.EmployeeId,
                    MaterialId = o.MaterialId,
                    SurfaceFinishId = o.SurfaceFinishId
                })
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves an order by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order.</param>
        /// <returns>The <see cref="OrderDto"/> object if found, otherwise null.</returns>
        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            return await _context.Order
                .Where(o => o.Id == id)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description,
                    Quantity = o.Quantity,
                    UnitPrice = o.UnitPrice,
                    DiscountPercent = o.DiscountPercent,
                    Manufactured = o.Manufactured,
                    Subtotal = o.Subtotal,
                    Remaining = o.Remaining,
                    CreatedDate = o.CreatedDate,
                    ModifiedDate = o.ModifiedDate,
                    PromisedDate = o.PromisedDate,
                    FinishedDate = o.FinishedDate,
                    Turnaround = o.Turnaround,
                    TrackingNumber = o.TrackingNumber,
                    ProcessId = o.ProcessId,
                    ColorId = o.ColorId,
                    CurrencyId = o.CurrencyId,
                    CustomerId = o.CustomerId,
                    EmployeeId = o.EmployeeId,
                    MaterialId = o.MaterialId,
                    SurfaceFinishId = o.SurfaceFinishId
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new order asynchronously.
        /// </summary>
        /// <param name="request">The request containing order data.</param>
        /// <returns>The newly created <see cref="OrderDto"/> object.</returns>
        public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request)
        {
            var order = new Order
            {
                Name = request.Name,
                Description = request.Description,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                DiscountPercent = request.DiscountPercent,
                Manufactured = request.Manufactured,
                PromisedDate = request.PromisedDate,
                FinishedDate = request.FinishedDate,
                TrackingNumber = request.TrackingNumber,
                ProcessId = request.ProcessId,
                ColorId = request.ColorId,
                CurrencyId = request.CurrencyId,
                CustomerId = request.CustomerId,
                EmployeeId = request.EmployeeId,
                MaterialId = request.MaterialId,
                SurfaceFinishId = request.SurfaceFinishId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            _context.Order.Add(order);
            await _context.SaveChangesAsync();
            return new OrderDto
            {
                Id = order.Id,
                Name = order.Name,
                Description = order.Description,
                Quantity = order.Quantity,
                UnitPrice = order.UnitPrice,
                DiscountPercent = order.DiscountPercent,
                Manufactured = order.Manufactured,
                Subtotal = order.Subtotal,
                Remaining = order.Remaining,
                CreatedDate = order.CreatedDate,
                ModifiedDate = order.ModifiedDate,
                PromisedDate = order.PromisedDate,
                FinishedDate = order.FinishedDate,
                Turnaround = order.Turnaround,
                TrackingNumber = order.TrackingNumber,
                ProcessId = order.ProcessId,
                ColorId = order.ColorId,
                CurrencyId = order.CurrencyId,
                CustomerId = order.CustomerId,
                EmployeeId = order.EmployeeId,
                MaterialId = order.MaterialId,
                SurfaceFinishId = order.SurfaceFinishId
            };
        }

        /// <summary>
        /// Updates an existing order asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated order data.</param>
        /// <returns>The updated <see cref="OrderDto"/> object if found, otherwise null.</returns>
        public async Task<OrderDto?> UpdateOrderAsync(UpdateOrderRequest request)
        {
            var order = await _context.Order.FindAsync(request.Id);
            if (order == null)
            {
                return null;
            }

            order.Name = request.Name;
            order.Description = request.Description;
            order.Quantity = request.Quantity;
            order.UnitPrice = request.UnitPrice;
            order.DiscountPercent = request.DiscountPercent;
            order.Manufactured = request.Manufactured;
            order.PromisedDate = request.PromisedDate;
            order.FinishedDate = request.FinishedDate;
            order.TrackingNumber = request.TrackingNumber;
            order.ProcessId = request.ProcessId;
            order.ColorId = request.ColorId;
            order.CurrencyId = request.CurrencyId;
            order.CustomerId = request.CustomerId;
            order.EmployeeId = request.EmployeeId;
            order.MaterialId = request.MaterialId;
            order.SurfaceFinishId = request.SurfaceFinishId;
            order.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new OrderDto
            {
                Id = order.Id,
                Name = order.Name,
                Description = order.Description,
                Quantity = order.Quantity,
                UnitPrice = order.UnitPrice,
                DiscountPercent = order.DiscountPercent,
                Manufactured = order.Manufactured,
                Subtotal = order.Subtotal,
                Remaining = order.Remaining,
                CreatedDate = order.CreatedDate,
                ModifiedDate = order.ModifiedDate,
                PromisedDate = order.PromisedDate,
                FinishedDate = order.FinishedDate,
                Turnaround = order.Turnaround,
                TrackingNumber = order.TrackingNumber,
                ProcessId = order.ProcessId,
                ColorId = order.ColorId,
                CurrencyId = order.CurrencyId,
                CustomerId = order.CustomerId,
                EmployeeId = order.EmployeeId,
                MaterialId = order.MaterialId,
                SurfaceFinishId = order.SurfaceFinishId
            };
        }

        /// <summary>
        /// Deletes an order by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order to delete.</param>
        /// <returns>True if the order was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return false;
            }

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Retrieves all order files asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="OrderFileDto"/> objects.</returns>
        public async Task<IEnumerable<OrderFileDto>> GetAllOrderFilesAsync()
        {
            return await _context.OrderFile
                .Select(of => new OrderFileDto { Id = of.Id, Bucket = of.Bucket, ObjectName = of.ObjectName, OrderId = of.OrderId })
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves an order file by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order file.</param>
        /// <returns>The <see cref="OrderFileDto"/> object if found, otherwise null.</returns>
        public async Task<OrderFileDto?> GetOrderFileByIdAsync(int id)
        {
            return await _context.OrderFile
                .Where(of => of.Id == id)
                .Select(of => new OrderFileDto { Id = of.Id, Bucket = of.Bucket, ObjectName = of.ObjectName, OrderId = of.OrderId })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new order file asynchronously.
        /// </summary>
        /// <param name="request">The request containing order file data.</param>
        /// <returns>The newly created <see cref="OrderFileDto"/> object.</returns>
        public async Task<OrderFileDto> CreateOrderFileAsync(CreateOrderFileRequest request)
        {
            var orderFile = new OrderFile { Bucket = request.Bucket, ObjectName = request.ObjectName, OrderId = request.OrderId, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow };
            _context.OrderFile.Add(orderFile);
            await _context.SaveChangesAsync();
            return new OrderFileDto { Id = orderFile.Id, Bucket = orderFile.Bucket, ObjectName = orderFile.ObjectName, OrderId = orderFile.OrderId };
        }

        /// <summary>
        /// Updates an existing order file asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated order file data.</param>
        /// <returns>The updated <see cref="OrderFileDto"/> object if found, otherwise null.</returns>
        public async Task<OrderFileDto?> UpdateOrderFileAsync(UpdateOrderFileRequest request)
        {
            var orderFile = await _context.OrderFile.FindAsync(request.Id);
            if (orderFile == null)
            {
                return null;
            }

            orderFile.Bucket = request.Bucket;
            orderFile.ObjectName = request.ObjectName;
            orderFile.OrderId = request.OrderId;
            orderFile.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new OrderFileDto { Id = orderFile.Id, Bucket = orderFile.Bucket, ObjectName = orderFile.ObjectName, OrderId = orderFile.OrderId };
        }

        /// <summary>
        /// Deletes an order file by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order file to delete.</param>
        /// <returns>True if the order file was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteOrderFileAsync(int id)
        {
            var orderFile = await _context.OrderFile.FindAsync(id);
            if (orderFile == null)
            {
                return false;
            }

            _context.OrderFile.Remove(orderFile);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Retrieves all processes asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="ProcessDto"/> objects.</returns>
        public async Task<IEnumerable<ProcessDto>> GetAllProcessesAsync()
        {
            return await _context.Process
                .Select(p => new ProcessDto { Id = p.Id, Name = p.Name, CategoryId = p.CategoryId })
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a process by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the process.</param>
        /// <returns>The <see cref="ProcessDto"/> object if found, otherwise null.</returns>
        public async Task<ProcessDto?> GetProcessByIdAsync(int id)
        {
            return await _context.Process
                .Where(p => p.Id == id)
                .Select(p => new ProcessDto { Id = p.Id, Name = p.Name, CategoryId = p.CategoryId })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new process asynchronously.
        /// </summary>
        /// <param name="request">The request containing process data.</param>
        /// <returns>The newly created <see cref="ProcessDto"/> object.</returns>
        public async Task<ProcessDto> CreateProcessAsync(CreateProcessRequest request)
        {
            var process = new Process { Name = request.Name, CategoryId = request.CategoryId, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow };
            _context.Process.Add(process);
            await _context.SaveChangesAsync();
            return new ProcessDto { Id = process.Id, Name = process.Name, CategoryId = process.CategoryId };
        }

        /// <summary>
        /// Updates an existing process asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated process data.</param>
        /// <returns>The updated <see cref="ProcessDto"/> object if found, otherwise null.</returns>
        public async Task<ProcessDto?> UpdateProcessAsync(UpdateProcessRequest request)
        {
            var process = await _context.Process.FindAsync(request.Id);
            if (process == null)
            {
                return null;
            }

            process.Name = request.Name;
            process.CategoryId = request.CategoryId;
            process.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new ProcessDto { Id = process.Id, Name = process.Name, CategoryId = process.CategoryId };
        }

        /// <summary>
        /// Deletes a process by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the process to delete.</param>
        /// <returns>True if the process was deleted successfully, otherwise false.</returns>
        public async Task<bool> DeleteProcessAsync(int id)
        {
            var process = await _context.Process.FindAsync(id);
            if (process == null)
            {
                return false;
            }

            _context.Process.Remove(process);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
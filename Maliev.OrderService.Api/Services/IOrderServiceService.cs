using Maliev.OrderService.Api.Models.DTOs;
using Maliev.OrderService.Data.Models;

namespace Maliev.OrderService.Api.Services
{
    /// <summary>
    /// Defines the interface for the Order Service, providing methods for managing categories, file formats, orders, order files, and processes.
    /// </summary>
    public interface IOrderServiceService
    {
        /// <summary>
        /// Retrieves all categories asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="CategoryDto"/> objects.</returns>
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();

        /// <summary>
        /// Retrieves a category by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the category.</param>
        /// <returns>The <see cref="CategoryDto"/> object if found, otherwise null.</returns>
        Task<CategoryDto?> GetCategoryByIdAsync(int id);

        /// <summary>
        /// Creates a new category asynchronously.
        /// </summary>
        /// <param name="request">The request containing category data.</param>
        /// <returns>The newly created <see cref="CategoryDto"/> object.</returns>
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);

        /// <summary>
        /// Updates an existing category asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated category data.</param>
        /// <returns>The updated <see cref="CategoryDto"/> object if found, otherwise null.</returns>
        Task<CategoryDto?> UpdateCategoryAsync(UpdateCategoryRequest request);

        /// <summary>
        /// Deletes a category by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the category to delete.</param>
        /// <returns>True if the category was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteCategoryAsync(int id);

        /// <summary>
        /// Retrieves all file formats asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="FileFormatDto"/> objects.</returns>
        Task<IEnumerable<FileFormatDto>> GetAllFileFormatsAsync();

        /// <summary>
        /// Retrieves a file format by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the file format.</param>
        /// <returns>The <see cref="FileFormatDto"/> object if found, otherwise null.</returns>
        Task<FileFormatDto?> GetFileFormatByIdAsync(int id);

        /// <summary>
        /// Creates a new file format asynchronously.
        /// </summary>
        /// <param name="request">The request containing file format data.</param>
        /// <returns>The newly created <see cref="FileFormatDto"/> object.</returns>
        Task<FileFormatDto> CreateFileFormatAsync(CreateFileFormatRequest request);

        /// <summary>
        /// Updates an existing file format asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated file format data.</param>
        /// <returns>The updated <see cref="FileFormatDto"/> object if found, otherwise null.</returns>
        Task<FileFormatDto?> UpdateFileFormatAsync(UpdateFileFormatRequest request);

        /// <summary>
        /// Deletes a file format by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the file format to delete.</param>
        /// <returns>True if the file format was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteFileFormatAsync(int id);

        /// <summary>
        /// Retrieves all orders asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="OrderDto"/> objects.</returns>
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();

        /// <summary>
        /// Retrieves an order by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order.</param>
        /// <returns>The <see cref="OrderDto"/> object if found, otherwise null.</returns>
        Task<OrderDto?> GetOrderByIdAsync(int id);

        /// <summary>
        /// Creates a new order asynchronously.
        /// </summary>
        /// <param name="request">The request containing order data.</param>
        /// <returns>The newly created <see cref="OrderDto"/> object.</returns>
        Task<OrderDto> CreateOrderAsync(CreateOrderRequest request);

        /// <summary>
        /// Updates an existing order asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated order data.</param>
        /// <returns>The updated <see cref="OrderDto"/> object if found, otherwise null.</returns>
        Task<OrderDto?> UpdateOrderAsync(UpdateOrderRequest request);

        /// <summary>
        /// Deletes an order by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order to delete.</param>
        /// <returns>True if the order was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteOrderAsync(int id);

        /// <summary>
        /// Retrieves all order files asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="OrderFileDto"/> objects.</returns>
        Task<IEnumerable<OrderFileDto>> GetAllOrderFilesAsync();

        /// <summary>
        /// Retrieves an order file by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order file.</param>
        /// <returns>The <see cref="OrderFileDto"/> object if found, otherwise null.</returns>
        Task<OrderFileDto?> GetOrderFileByIdAsync(int id);

        /// <summary>
        /// Creates a new order file asynchronously.
        /// </summary>
        /// <param name="request">The request containing order file data.</param>
        /// <returns>The newly created <see cref="OrderFileDto"/> object.</returns>
        Task<OrderFileDto> CreateOrderFileAsync(CreateOrderFileRequest request);

        /// <summary>
        /// Updates an existing order file asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated order file data.</param>
        /// <returns>The updated <see cref="OrderFileDto"/> object if found, otherwise null.</returns>
        Task<OrderFileDto?> UpdateOrderFileAsync(UpdateOrderFileRequest request);

        /// <summary>
        /// Deletes an order file by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the order file to delete.</param>
        /// <returns>True if the order file was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteOrderFileAsync(int id);

        /// <summary>
        /// Retrieves all processes asynchronously.
        /// </summary>
        /// <returns>A collection of <see cref="ProcessDto"/> objects.</returns>
        Task<IEnumerable<ProcessDto>> GetAllProcessesAsync();

        /// <summary>
        /// Retrieves a process by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the process.</param>
        /// <returns>The <see cref="ProcessDto"/> object if found, otherwise null.</returns>
        Task<ProcessDto?> GetProcessByIdAsync(int id);

        /// <summary>
        /// Creates a new process asynchronously.
        /// </summary>
        /// <param name="request">The request containing process data.</param>
        /// <returns>The newly created <see cref="ProcessDto"/> object.</returns>
        Task<ProcessDto> CreateProcessAsync(CreateProcessRequest request);

        /// <summary>
        /// Updates an existing process asynchronously.
        /// </summary>
        /// <param name="request">The request containing updated process data.</param>
        /// <returns>The updated <see cref="ProcessDto"/> object if found, otherwise null.</returns>
        Task<ProcessDto?> UpdateProcessAsync(UpdateProcessRequest request);

        /// <summary>
        /// Deletes a process by its ID asynchronously.
        /// </summary>
        /// <param name="id">The ID of the process to delete.</param>
        /// <returns>True if the process was deleted successfully, otherwise false.</returns>
        Task<bool> DeleteProcessAsync(int id);
    }
}
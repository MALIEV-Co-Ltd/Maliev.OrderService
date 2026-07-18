namespace Maliev.OrderService.Application.DTOs.Response
{
    /// <summary>
    /// Generic paginated response wrapper.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>Gets or sets the items on the current page.</summary>
        public required List<T> Items { get; set; }

        /// <summary>Gets or sets the current page number.</summary>
        public int Page { get; set; }

        /// <summary>Gets or sets the page size.</summary>
        public int PageSize { get; set; }

        /// <summary>Gets or sets the total number of items.</summary>
        public int TotalCount { get; set; }

        /// <summary>Gets or sets the total number of pages.</summary>
        public int TotalPages { get; set; }
    }
}

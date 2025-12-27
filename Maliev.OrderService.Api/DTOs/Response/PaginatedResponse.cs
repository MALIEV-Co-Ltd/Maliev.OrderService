namespace Maliev.OrderService.Api.DTOs.Response
{
    /// <summary>
    /// Generic paginated response wrapper
    /// </summary>
    /// <typeparam name="T">The type of items in the response</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>Gets or sets the list of items for the current page</summary>
        public required List<T> Items { get; set; }

        /// <summary>Gets or sets the current page number</summary>
        public required int Page { get; set; }

        /// <summary>Gets or sets the page size</summary>
        public required int PageSize { get; set; }

        /// <summary>Gets or sets the total count of items across all pages</summary>
        public required int TotalCount { get; set; }

        /// <summary>Gets or sets the total number of pages</summary>
        public required int TotalPages { get; set; }

        /// <summary>Gets a value indicating whether there is a previous page</summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>Gets a value indicating whether there is a next page</summary>
        public bool HasNextPage => Page < TotalPages;
    }
}

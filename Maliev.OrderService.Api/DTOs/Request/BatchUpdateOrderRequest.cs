namespace Maliev.OrderService.Api.DTOs.Request
{
    /// <summary>
    /// Request model for batch updating orders
    /// </summary>
    public class BatchUpdateOrderRequest
    {
        /// <summary>Gets or sets the order ID</summary>
        public required string OrderId { get; set; }

        /// <summary>Gets or sets the order version (RowVersion)</summary>
        public required string Version { get; set; }

        /// <summary>Gets or sets the order requirements</summary>
        public string? Requirements { get; set; }

        /// <summary>Gets or sets the ordered quantity</summary>
        public int? OrderedQuantity { get; set; }

        /// <summary>Gets or sets the assigned employee ID</summary>
        public string? AssignedEmployeeId { get; set; }

        /// <summary>Gets or sets the department ID</summary>
        public string? DepartmentId { get; set; }
    }
}

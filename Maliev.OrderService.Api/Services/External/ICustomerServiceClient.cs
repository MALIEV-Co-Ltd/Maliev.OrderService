namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Interface for interacting with the external Customer Service
    /// </summary>
    public interface ICustomerServiceClient
    {
        /// <summary>
        /// Checks if a customer has an active NDA
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the customer has an active NDA, false otherwise</returns>
        Task<bool> HasActiveNdaAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets customer details by ID
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Customer details or null if not found</returns>
        Task<CustomerDetailsDto?> GetCustomerDetailsAsync(string customerId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Data transfer object for customer details
    /// </summary>
    public class CustomerDetailsDto
    {
        /// <summary>Gets or sets the customer ID</summary>
        public required string CustomerId { get; set; }

        /// <summary>Gets or sets the customer name</summary>
        public required string Name { get; set; }

        /// <summary>Gets or sets the customer email</summary>
        public required string Email { get; set; }

        /// <summary>Gets or sets whether the customer has an active NDA</summary>
        public bool HasActiveNda { get; set; }
    }
}

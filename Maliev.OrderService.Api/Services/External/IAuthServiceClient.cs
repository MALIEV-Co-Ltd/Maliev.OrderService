namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Interface for interacting with the external Auth Service
    /// </summary>
    public interface IAuthServiceClient
    {
        /// <summary>
        /// Validates an authentication token
        /// </summary>
        /// <param name="token">The authentication token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The user context if valid, null otherwise</returns>
        Task<UserContextDto?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Data transfer object for user context
    /// </summary>
    public class UserContextDto
    {
        /// <summary>Gets or sets the user type (e.g., "customer" or "employee")</summary>
        public required string UserType { get; set; }

        /// <summary>Gets or sets the user ID</summary>
        public required string UserId { get; set; }

        /// <summary>Gets or sets the user roles</summary>
        public List<string> Roles { get; set; } = [];

        /// <summary>Gets or sets the department ID (if applicable)</summary>
        public string? DepartmentId { get; set; }
    }
}

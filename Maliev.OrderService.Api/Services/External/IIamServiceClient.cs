namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Interface for interacting with the central IAM service.
    /// </summary>
    public interface IIamServiceClient
    {
        /// <summary>
        /// Gets all active permissions for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of permission strings (e.g., "order.orders.create").</returns>
        Task<IEnumerable<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    }
}

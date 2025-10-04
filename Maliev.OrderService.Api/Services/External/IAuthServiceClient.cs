namespace Maliev.OrderService.Api.Services.External;

public interface IAuthServiceClient
{
    Task<UserContextDto?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}

public class UserContextDto
{
    public required string UserType { get; set; } // "customer" or "employee"
    public required string UserId { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? DepartmentId { get; set; }
}

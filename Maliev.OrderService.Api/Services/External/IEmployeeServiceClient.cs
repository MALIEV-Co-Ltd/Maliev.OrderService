namespace Maliev.OrderService.Api.Services.External;

public interface IEmployeeServiceClient
{
    Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
}

public class EmployeeDetailsDto
{
    public required string EmployeeId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}

public class DepartmentDto
{
    public required string DepartmentId { get; set; }
    public required string Name { get; set; }
}

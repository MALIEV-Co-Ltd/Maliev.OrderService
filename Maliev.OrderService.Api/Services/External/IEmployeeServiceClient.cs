namespace Maliev.OrderService.Api.Services.External;

/// <summary>
/// Interface for interacting with the external Employee Service
/// </summary>
public interface IEmployeeServiceClient
{
    /// <summary>
    /// Gets employee details by ID
    /// </summary>
    /// <param name="employeeId">The employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Employee details or null if not found</returns>
    Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all departments
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of departments</returns>
    Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for employee details
/// </summary>
public class EmployeeDetailsDto
{
    /// <summary>Gets or sets the employee ID</summary>
    public required string EmployeeId { get; set; }
    
    /// <summary>Gets or sets the employee name</summary>
    public required string Name { get; set; }
    
    /// <summary>Gets or sets the employee email</summary>
    public required string Email { get; set; }
    
    /// <summary>Gets or sets the department ID</summary>
    public string? DepartmentId { get; set; }
    
    /// <summary>Gets or sets the department name</summary>
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Data transfer object for department details
/// </summary>
public class DepartmentDto
{
    /// <summary>Gets or sets the department ID</summary>
    public required string DepartmentId { get; set; }
    
    /// <summary>Gets or sets the department name</summary>
    public required string Name { get; set; }
}

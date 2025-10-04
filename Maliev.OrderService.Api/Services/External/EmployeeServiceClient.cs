using System.Net.Http.Json;

namespace Maliev.OrderService.Api.Services.External;

public partial class EmployeeServiceClient : IEmployeeServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmployeeServiceClient> _logger;

    public EmployeeServiceClient(HttpClient httpClient, ILogger<EmployeeServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/employees/{employeeId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<EmployeeDetailsDto>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToGetEmployeeDetails(_logger, employeeId, ex);
            return null;
        }
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/departments", cancellationToken);
            response.EnsureSuccessStatusCode();

            var departments = await response.Content.ReadFromJsonAsync<List<DepartmentDto>>(cancellationToken: cancellationToken);
            return departments ?? new List<DepartmentDto>();
        }
        catch (HttpRequestException ex)
        {
            Log.FailedToGetDepartments(_logger, ex);
            return new List<DepartmentDto>();
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get employee details for {EmployeeId}")]
        public static partial void FailedToGetEmployeeDetails(ILogger logger, string employeeId, Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get departments list")]
        public static partial void FailedToGetDepartments(ILogger logger, Exception ex);
    }
}

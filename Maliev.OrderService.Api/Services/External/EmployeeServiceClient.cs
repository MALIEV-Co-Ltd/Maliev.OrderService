namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the external Employee Service
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="EmployeeServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="logger">The logger instance</param>
    public partial class EmployeeServiceClient(HttpClient httpClient, ILogger<EmployeeServiceClient> logger) : IEmployeeServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<EmployeeServiceClient> _logger = logger;

        /// <inheritdoc />
        public async Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(string employeeId, CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/employees/{employeeId}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<EmployeeDetailsDto>(cancellationToken: cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToGetEmployeeDetails(_logger, employeeId, ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("/api/v1/departments", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                List<DepartmentDto>? departments = await response.Content.ReadFromJsonAsync<List<DepartmentDto>>(cancellationToken: cancellationToken);
                return departments ?? [];
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToGetDepartments(_logger, ex);
                return [];
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
}

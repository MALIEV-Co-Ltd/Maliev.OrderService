namespace Maliev.OrderService.Api.DTOs.Response;

public class BatchOperationResponse
{
    public required int TotalRequested { get; set; }
    public required int SuccessCount { get; set; }
    public required int FailureCount { get; set; }
    public List<BatchOperationError>? Errors { get; set; }
}

public class BatchOperationError
{
    public required int Index { get; set; }
    public required string OrderId { get; set; }
    public required string ErrorMessage { get; set; }
}

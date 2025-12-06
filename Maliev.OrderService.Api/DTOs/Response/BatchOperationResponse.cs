namespace Maliev.OrderService.Api.DTOs.Response;

/// <summary>
/// Response model for batch operation results
/// </summary>
public class BatchOperationResponse
{
    /// <summary>Gets or sets the total number of requests submitted</summary>
    public required int TotalRequested { get; set; }
    
    /// <summary>Gets or sets the number of successful operations</summary>
    public required int SuccessCount { get; set; }
    
    /// <summary>Gets or sets the number of failed operations</summary>
    public required int FailureCount { get; set; }
    
    /// <summary>Gets or sets the list of errors for failed operations</summary>
    public List<BatchOperationError>? Errors { get; set; }
}

/// <summary>
/// Error information for a single failed batch operation
/// </summary>
public class BatchOperationError
{
    /// <summary>Gets or sets the index of the failed item in the batch</summary>
    public required int Index { get; set; }
    
    /// <summary>Gets or sets the order ID that failed</summary>
    public required string OrderId { get; set; }
    
    /// <summary>Gets or sets the error message</summary>
    public required string ErrorMessage { get; set; }
}

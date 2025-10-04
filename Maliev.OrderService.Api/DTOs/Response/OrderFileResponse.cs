namespace Maliev.OrderService.Api.DTOs.Response;

public class OrderFileResponse
{
    public required long FileId { get; set; }
    public required string OrderId { get; set; }
    public required string FileName { get; set; }
    public required string FileRole { get; set; }
    public required string FileCategory { get; set; }
    public required long FileSize { get; set; }
    public required string FileType { get; set; }
    public required string ObjectPath { get; set; } // Path in Upload Service
    public required string AccessLevel { get; set; }
    public string? DesignUnits { get; set; }
    public required string UploadedBy { get; set; }
    public required DateTime UploadedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

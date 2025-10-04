namespace Maliev.OrderService.Api.DTOs.Request;

public class UploadOrderFileRequest
{
    public required string FileRole { get; set; } // Input, Output, Supporting
    public required string FileCategory { get; set; } // CAD, Drawing, Image, Document, Archive, Other
    public string? DesignUnits { get; set; } // mm, inch, cm, m (CAD files only)
}

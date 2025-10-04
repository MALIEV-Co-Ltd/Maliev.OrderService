namespace Maliev.OrderService.Api.Services.External;

public interface IMaterialServiceClient
{
    Task<string?> GetMaterialNameAsync(int materialId, CancellationToken cancellationToken = default);
    Task<string?> GetColorNameAsync(int colorId, CancellationToken cancellationToken = default);
    Task<string?> GetSurfaceFinishingNameAsync(int surfaceFinishingId, CancellationToken cancellationToken = default);
}

public class MaterialDto
{
    public int MaterialId { get; set; }
    public required string Name { get; set; }
}

public class ColorDto
{
    public int ColorId { get; set; }
    public required string Name { get; set; }
}

public class SurfaceFinishingDto
{
    public int SurfaceFinishingId { get; set; }
    public required string Name { get; set; }
}

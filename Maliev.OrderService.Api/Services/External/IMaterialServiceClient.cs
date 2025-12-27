namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Interface for interacting with the external Material Service
    /// </summary>
    public interface IMaterialServiceClient
    {
        /// <summary>
        /// Gets the name of a material by its ID
        /// </summary>
        /// <param name="materialId">The material ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The material name or null if not found</returns>
        Task<string?> GetMaterialNameAsync(int materialId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name of a color by its ID
        /// </summary>
        /// <param name="colorId">The color ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The color name or null if not found</returns>
        Task<string?> GetColorNameAsync(int colorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name of a surface finishing by its ID
        /// </summary>
        /// <param name="surfaceFinishingId">The surface finishing ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The surface finishing name or null if not found</returns>
        Task<string?> GetSurfaceFinishingNameAsync(int surfaceFinishingId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Data transfer object for material information
    /// </summary>
    public class MaterialDto
    {
        /// <summary>Gets or sets the material ID</summary>
        public int MaterialId { get; set; }

        /// <summary>Gets or sets the material name</summary>
        public required string Name { get; set; }
    }

    /// <summary>
    /// Data transfer object for color information
    /// </summary>
    public class ColorDto
    {
        /// <summary>Gets or sets the color ID</summary>
        public int ColorId { get; set; }

        /// <summary>Gets or sets the color name</summary>
        public required string Name { get; set; }
    }

    /// <summary>
    /// Data transfer object for surface finishing information
    /// </summary>
    public class SurfaceFinishingDto
    {
        /// <summary>Gets or sets the surface finishing ID</summary>
        public int SurfaceFinishingId { get; set; }

        /// <summary>Gets or sets the surface finishing name</summary>
        public required string Name { get; set; }
    }
}

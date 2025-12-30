using Microsoft.Extensions.Caching.Memory;

namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the external Material Service
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MaterialServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="cache">The memory cache</param>
    /// <param name="logger">The logger instance</param>
    public partial class MaterialServiceClient(HttpClient httpClient, IMemoryCache cache, ILogger<MaterialServiceClient> logger) : IMaterialServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IMemoryCache _cache = cache;
        private readonly ILogger<MaterialServiceClient> _logger = logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

        /// <inheritdoc />
        public async Task<string?> GetMaterialNameAsync(int materialId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"material_{materialId}";

            if (_cache.TryGetValue(cacheKey, out string? cachedName))
            {
                return cachedName;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/materials/{materialId}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                MaterialDto? material = await response.Content.ReadFromJsonAsync<MaterialDto>(cancellationToken: cancellationToken);
                if (material != null)
                {
                    _ = _cache.Set(cacheKey, material.Name, CacheDuration);
                    return material.Name;
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToGetMaterialName(_logger, materialId, ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetColorNameAsync(int colorId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"color_{colorId}";

            if (_cache.TryGetValue(cacheKey, out string? cachedName))
            {
                return cachedName;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/colors/{colorId}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                ColorDto? color = await response.Content.ReadFromJsonAsync<ColorDto>(cancellationToken: cancellationToken);
                if (color != null)
                {
                    _ = _cache.Set(cacheKey, color.Name, CacheDuration);
                    return color.Name;
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToGetColorName(_logger, colorId, ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetSurfaceFinishingNameAsync(int surfaceFinishingId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"surface_{surfaceFinishingId}";

            if (_cache.TryGetValue(cacheKey, out string? cachedName))
            {
                return cachedName;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/surface-finishings/{surfaceFinishingId}", cancellationToken);
                _ = response.EnsureSuccessStatusCode();

                SurfaceFinishingDto? surface = await response.Content.ReadFromJsonAsync<SurfaceFinishingDto>(cancellationToken: cancellationToken);
                if (surface != null)
                {
                    _ = _cache.Set(cacheKey, surface.Name, CacheDuration);
                    return surface.Name;
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                Log.FailedToGetSurfaceFinishingName(_logger, surfaceFinishingId, ex);
                return null;
            }
        }

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get material name for ID {MaterialId}")]
            public static partial void FailedToGetMaterialName(ILogger logger, int materialId, Exception ex);

            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get color name for ID {ColorId}")]
            public static partial void FailedToGetColorName(ILogger logger, int colorId, Exception ex);

            [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get surface finishing name for ID {SurfaceFinishingId}")]
            public static partial void FailedToGetSurfaceFinishingName(ILogger logger, int surfaceFinishingId, Exception ex);
        }
    }
}

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Maliev.OrderService.Api.Services.External
{
    /// <summary>
    /// Client for interacting with the external Material Service
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MaterialServiceClient"/> class.
    /// </remarks>
    /// <param name="httpClient">The HTTP client</param>
    /// <param name="cache">The distributed cache</param>
    /// <param name="logger">The logger instance</param>
    public partial class MaterialServiceClient(HttpClient httpClient, IDistributedCache cache, ILogger<MaterialServiceClient> logger) : IMaterialServiceClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<MaterialServiceClient> _logger = logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

        /// <inheritdoc />
        public async Task<string?> GetMaterialNameAsync(int materialId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"material:{materialId}";
            string? cachedName = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (cachedName != null)
            {
                return cachedName;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/materials/{materialId}", cancellationToken);
                if (!response.IsSuccessStatusCode) return null;

                MaterialDto? material = await response.Content.ReadFromJsonAsync<MaterialDto>(cancellationToken: cancellationToken);
                if (material != null)
                {
                    await _cache.SetStringAsync(cacheKey, material.Name, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, cancellationToken);
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
            string cacheKey = $"color:{colorId}";
            string? cachedName = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (cachedName != null)
            {
                return cachedName;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/colors/{colorId}", cancellationToken);
                if (!response.IsSuccessStatusCode) return null;

                ColorDto? color = await response.Content.ReadFromJsonAsync<ColorDto>(cancellationToken: cancellationToken);
                if (color != null)
                {
                    await _cache.SetStringAsync(cacheKey, color.Name, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, cancellationToken);
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
            string cacheKey = $"surface:{surfaceFinishingId}";
            string? cachedName = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (cachedName != null)
            {
                return cachedName;
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"/api/v1/surface-finishings/{surfaceFinishingId}", cancellationToken);
                if (!response.IsSuccessStatusCode) return null;

                SurfaceFinishingDto? surface = await response.Content.ReadFromJsonAsync<SurfaceFinishingDto>(cancellationToken: cancellationToken);
                if (surface != null)
                {
                    await _cache.SetStringAsync(cacheKey, surface.Name, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, cancellationToken);
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

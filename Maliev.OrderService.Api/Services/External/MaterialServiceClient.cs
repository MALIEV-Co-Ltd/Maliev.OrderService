using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Maliev.OrderService.Api.Services.External;

public partial class MaterialServiceClient : IMaterialServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MaterialServiceClient> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public MaterialServiceClient(HttpClient httpClient, IMemoryCache cache, ILogger<MaterialServiceClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetMaterialNameAsync(int materialId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"material_{materialId}";

        if (_cache.TryGetValue<string>(cacheKey, out var cachedName))
        {
            return cachedName;
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/materials/{materialId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var material = await response.Content.ReadFromJsonAsync<MaterialDto>(cancellationToken: cancellationToken);
            if (material != null)
            {
                _cache.Set(cacheKey, material.Name, CacheDuration);
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

    public async Task<string?> GetColorNameAsync(int colorId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"color_{colorId}";

        if (_cache.TryGetValue<string>(cacheKey, out var cachedName))
        {
            return cachedName;
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/colors/{colorId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var color = await response.Content.ReadFromJsonAsync<ColorDto>(cancellationToken: cancellationToken);
            if (color != null)
            {
                _cache.Set(cacheKey, color.Name, CacheDuration);
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

    public async Task<string?> GetSurfaceFinishingNameAsync(int surfaceFinishingId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"surface_{surfaceFinishingId}";

        if (_cache.TryGetValue<string>(cacheKey, out var cachedName))
        {
            return cachedName;
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/surface-finishings/{surfaceFinishingId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var surface = await response.Content.ReadFromJsonAsync<SurfaceFinishingDto>(cancellationToken: cancellationToken);
            if (surface != null)
            {
                _cache.Set(cacheKey, surface.Name, CacheDuration);
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

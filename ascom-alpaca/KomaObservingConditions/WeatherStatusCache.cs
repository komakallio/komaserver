using KomaObservingConditions.WeatherRestApi;
using Microsoft.Extensions.Caching.Memory;

namespace KomaObservingConditions;

internal class WeatherStatusCache(
    IMemoryCache memoryCache,
    IWeatherApi api) : IWeatherStatusSource
{
    private const string CacheKey = "WeatherStatus";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<WeatherStatus?> GetStatusAsync()
    {
        if (memoryCache.TryGetValue(CacheKey, out WeatherStatus? cached))
            return cached;

        // The semaphor provides stampede protection, ensuring only one request fetches the data when cache is expired
        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (memoryCache.TryGetValue(CacheKey, out cached))
                return cached;

            var status = await api.GetWeatherStatusAsync();

            memoryCache.Set(CacheKey, status, CacheDuration);
            return status;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
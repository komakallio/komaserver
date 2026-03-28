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

    public async Task<TimestampedResult<WeatherStatus>> GetStatusAsync()
    {
        if (memoryCache.TryGetValue(CacheKey, out TimestampedResult<WeatherStatus>? cached))
            return cached!;

        // The semaphor provides stampede protection, ensuring only one request fetches the data when cache is expired
        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (memoryCache.TryGetValue(CacheKey, out cached))
                return cached!;

            var status = await api.GetWeatherStatusAsync();
            var result = new TimestampedResult<WeatherStatus>(status, DateTime.UtcNow);

            memoryCache.Set(CacheKey, result, CacheDuration);
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
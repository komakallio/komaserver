using KomaSafetyMonitor.SafetyRestApi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KomaSafetyMonitor;

internal class SafetyStatusCache(
    IMemoryCache memoryCache,
    ISafetyMonitorApi api,
    ILogger<SafetyStatusCache> logger) : ISafetyStatusSource
{
    private const string CacheKey = "SafetyStatus";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<TimestampedResult<SafetyStatus>> GetStatusAsync()
    {
        if (memoryCache.TryGetValue(CacheKey, out TimestampedResult<SafetyStatus>? cached))
        {
            return cached!;
        }

        // The semaphor provides stampede protection, ensuring only one request fetches the data when cache is expired
        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (memoryCache.TryGetValue(CacheKey, out cached))
            {
                logger.LogDebug("Fetched safety status from cache");
                return cached!;
            }

            var status = await api.GetSafetyStatusAsync();
            var result = new TimestampedResult<SafetyStatus>(status, DateTime.UtcNow);

            memoryCache.Set(CacheKey, result, CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch safety status");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

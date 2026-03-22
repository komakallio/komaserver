using KomaAlpacaCommon;
using KomaSafetyMonitor.SafetyRestApi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KomaSafetyMonitor
{
    public class SafetyStatusCache(
        IMemoryCache memoryCache,
        IRefitClientFactory<ISafetyMonitorApi> refitClientFactory,
        IOptions<SafetyMonitorOptions> options) : ISafetyStatusSource
    {
        private const string CacheKey = "SafetyStatus";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<SafetyStatus?> GetStatusAsync()
        {
            if (memoryCache.TryGetValue(CacheKey, out SafetyStatus? cached))
                return cached;

            // The semaphor provides stampede protection, ensuring only one request fetches the data when cache is expired
            await _semaphore.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (memoryCache.TryGetValue(CacheKey, out cached))
                    return cached;

                var status = await refitClientFactory
                    .CreateClient(options.Value.BaseUrl)
                    .GetSafetyStatusAsync();

                memoryCache.Set(CacheKey, status, CacheDuration);
                return status;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

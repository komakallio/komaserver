using Refit;

namespace KomaSafetyMonitor.SafetyRestApi;

internal interface ISafetyMonitorApi
{
    [Get("/safety")]
    public Task<SafetyStatus> GetSafetyStatusAsync();
}

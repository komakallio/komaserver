using Refit;

namespace KomaSafetyMonitor.SafetyRestApi;

public interface ISafetyMonitorApi
{
    [Get("/safety")]
    public Task<SafetyStatus> GetSafetyStatusAsync();
}

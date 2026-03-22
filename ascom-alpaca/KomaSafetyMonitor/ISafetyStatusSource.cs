using KomaSafetyMonitor.SafetyRestApi;

namespace KomaSafetyMonitor;

public interface ISafetyStatusSource
{
    Task<SafetyStatus?> GetStatusAsync();
}
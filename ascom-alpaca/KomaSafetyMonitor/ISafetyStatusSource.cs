using KomaSafetyMonitor.SafetyRestApi;

namespace KomaSafetyMonitor;

public interface ISafetyStatusSource
{
    Task<TimestampedResult<SafetyStatus>> GetStatusAsync();
}
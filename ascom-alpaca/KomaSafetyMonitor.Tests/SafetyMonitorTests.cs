using KomaSafetyMonitor.SafetyRestApi;
using Moq;
using Xunit;
using static KomaSafetyMonitor.SafetyRestApi.SafetyStatus;

namespace KomaSafetyMonitor.Tests;

public sealed class SafetyMonitorTests : IDisposable
{
    private readonly Mock<ISafetyStatusSource> _source;
    private readonly SafetyMonitor _monitor;

    public SafetyMonitorTests()
    {
        _source = new Mock<ISafetyStatusSource>();
        _monitor = new SafetyMonitor(_source.Object);
    }

    public void Dispose()
    {
        _monitor.Dispose();
    }

    private static SafetyStatus CreateSafetyStatus(bool safe) => new()
    {
        Safe = safe,
        Details = new SafetyStatusDetails
        {
            Temperature = new SafetyValue { Value = 15.0, Safe = safe },
            RainIntensity = new SafetyValue { Value = 0.0, Safe = safe },
            RainTrigger = new SafetyValue { Value = 0.0, Safe = safe },
            RainRadar10Km = new SafetyValue { Value = 0.0, Safe = safe },
            RainRadar30Km = new SafetyValue { Value = 0.0, Safe = safe },
            RainRadar50Km = new SafetyValue { Value = 0.0, Safe = safe },
            SunAltitude = new SafetyValue { Value = -15.0, Safe = safe },
            MoonAltitude = new SafetyValue { Value = 30.0, Safe = safe },
            UpsCharge = new SafetyValue { Value = 100.0, Safe = safe },
            EnclosureTemp = new SafetyValue { Value = 20.0, Safe = safe },
        }
    };

    #region IsSafe

    [Fact]
    public void IsSafe_WhenStatusIsSafe_ReturnsTrue()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(new TimestampedResult<SafetyStatus>(CreateSafetyStatus(true), DateTime.UtcNow));

        Assert.True(_monitor.IsSafe);
    }

    [Fact]
    public void IsSafe_WhenStatusIsUnsafe_ReturnsFalse()
    {
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(new TimestampedResult<SafetyStatus>(CreateSafetyStatus(false), DateTime.UtcNow));

        Assert.False(_monitor.IsSafe);
    }

    #endregion

    #region DeviceState

    [Fact]
    public void DeviceState_ReturnsIsSafeAndTimeStamp()
    {
        var fetchedAt = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        _source.Setup(s => s.GetStatusAsync()).ReturnsAsync(new TimestampedResult<SafetyStatus>(CreateSafetyStatus(true), fetchedAt));

        var state = _monitor.DeviceState;

        Assert.NotEmpty(state);
        Assert.Contains(state, s => s.Name == "IsSafe" && (bool)s.Value == true);
        Assert.Contains(state, s => s.Name == "TimeStamp" && s.Value.ToString() == "2025-01-15T12:00:00.0000000Z");
    }

    #endregion
}

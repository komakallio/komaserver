using KomaAlpacaCommon;
using KomaSafetyMonitor.SafetyRestApi;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static KomaSafetyMonitor.SafetyRestApi.SafetyStatus;

namespace KomaSafetyMonitor.Tests;

public class SafetyMonitorTests : IDisposable
{
    private readonly Mock<ISafetyMonitorApi> _api;
    private readonly SafetyMonitor _monitor;

    public SafetyMonitorTests()
    {
        var factory = new Mock<IRefitClientFactory<ISafetyMonitorApi>>();
        _api = new Mock<ISafetyMonitorApi>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_api.Object);
        _monitor = new SafetyMonitor(factory.Object, Options.Create<SafetyMonitorOptions>(new() { BaseUrl = "https://invalid.invalid" }));
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

    #region Connection

    [Fact]
    public void Connect_SetsConnectedTrue()
    {
        _api.Setup(a => a.GetSafetyStatusAsync()).ReturnsAsync(CreateSafetyStatus(true));

        _monitor.Connected = true;

        Assert.True(_monitor.Connected);
        Assert.False(_monitor.Connecting);
    }

    [Fact]
    public void Disconnect_AfterConnect_SetsConnectedFalse()
    {
        _api.Setup(a => a.GetSafetyStatusAsync()).ReturnsAsync(CreateSafetyStatus(true));
        _monitor.Connected = true;

        _monitor.Connected = false;

        Assert.False(_monitor.Connected);
    }

    [Fact]
    public void Connect_WhenAlreadyConnected_DoesNotCallApiAgain()
    {
        _api.Setup(a => a.GetSafetyStatusAsync()).ReturnsAsync(CreateSafetyStatus(true));
        _monitor.Connected = true;

        _monitor.Connected = true;

        _api.Verify(a => a.GetSafetyStatusAsync(), Times.Once);
    }

    #endregion

    #region IsSafe

    [Fact]
    public void IsSafe_WhenConnectedAndSafe_ReturnsTrue()
    {
        _api.Setup(a => a.GetSafetyStatusAsync()).ReturnsAsync(CreateSafetyStatus(true));

        _monitor.Connected = true;

        Assert.True(_monitor.IsSafe);
    }

    [Fact]
    public void IsSafe_WhenConnectedAndUnsafe_ReturnsFalse()
    {
        _api.Setup(a => a.GetSafetyStatusAsync()).ReturnsAsync(CreateSafetyStatus(false));

        _monitor.Connected = true;

        Assert.False(_monitor.IsSafe);
    }

    [Fact]
    public void IsSafe_WhenNotConnected_ReturnsFalse()
    {
        Assert.False(_monitor.IsSafe);
    }

    #endregion

    #region DeviceState

    [Fact]
    public void DeviceState_WhenConnected_ReturnsValues()
    {
        _api.Setup(a => a.GetSafetyStatusAsync()).ReturnsAsync(CreateSafetyStatus(true));
        _monitor.Connected = true;

        var state = _monitor.DeviceState;

        Assert.NotEmpty(state);
        Assert.Contains(state, s => s.Name == "IsSafe");
        Assert.Contains(state, s => s.Name == "TimeStamp");
    }

    [Fact]
    public void DeviceState_WhenNotConnected_ReturnsEmpty()
    {
        var state = _monitor.DeviceState;

        Assert.Empty(state);
    }

    #endregion
}

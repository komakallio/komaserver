using ASCOM.Common.DeviceInterfaces;
using KomaDome.DomeRestApi;
using Moq;
using Xunit;

namespace KomaDome.Tests;

public class DomeTests : IDisposable
{
    private readonly Mock<IDomeApi> _api;
    private readonly Dome _dome;

    public DomeTests()
    {
        _api = new Mock<IDomeApi>();
        _dome = new Dome(_api.Object, "testuser");
    }

    public void Dispose()
    {
        _dome.Dispose();
    }

    private void SetupStatusResponse(string state)
    {
        _api.Setup(a => a.GetStatusAsync("testuser")).ReturnsAsync(new RoofStatus { State = state });
    }

    #region Connection

    [Fact]
    public void Connected_AlwaysReturnsTrue()
    {
        Assert.True(_dome.Connected);
    }

    [Fact]
    public void Connecting_AlwaysReturnsFalse()
    {
        Assert.False(_dome.Connecting);
    }

    #endregion

    #region Shutter state parsing

    [Theory]
    [InlineData("OPEN", ShutterState.Open)]
    [InlineData("CLOSED", ShutterState.Closed)]
    [InlineData("OPENING", ShutterState.Opening)]
    [InlineData("CLOSING", ShutterState.Closing)]
    [InlineData("UNKNOWN", ShutterState.Error)]
    [InlineData("", ShutterState.Error)]
    public void Connect_ParsesShutterState(string state, ShutterState expected)
    {
        SetupStatusResponse(state);

        Assert.Equal(expected, _dome.ShutterStatus);
    }

    #endregion

    #region Shutter control

    [Fact]
    public void OpenShutter_CallsApi()
    {
        SetupStatusResponse("CLOSED");

        _dome.OpenShutter();

        _api.Verify(a => a.OpenAsync("testuser"), Times.Once);
    }

    [Fact]
    public void CloseShutter_CallsApi()
    {
        SetupStatusResponse("OPEN");

        _dome.CloseShutter();

        _api.Verify(a => a.CloseAsync("testuser"), Times.Once);
    }

    [Fact]
    public void AbortSlew_CallsStopOnApi()
    {
        SetupStatusResponse("OPENING");

        _dome.AbortSlew();

        _api.Verify(a => a.StopAsync("testuser"), Times.Once);
    }

    #endregion

    #region Slewing

    [Theory]
    [InlineData("OPENING", true)]
    [InlineData("CLOSING", true)]
    [InlineData("OPEN", false)]
    [InlineData("CLOSED", false)]
    public void Slewing_ReflectsShutterState(string state, bool expectedSlewing)
    {
        SetupStatusResponse(state);

        Assert.Equal(expectedSlewing, _dome.Slewing);
    }

    #endregion

    #region DeviceState

    [Fact]
    public void DeviceState_WhenConnected_ReturnsValues()
    {
        SetupStatusResponse("OPEN");

        var state = _dome.DeviceState;

        Assert.NotEmpty(state);
        Assert.Contains(state, s => s.Name == "ShutterStatus");
        Assert.Contains(state, s => s.Name == "Slewing");
        Assert.Contains(state, s => s.Name == "TimeStamp");
    }


    #endregion

    #region Capabilities

    [Fact]
    public void CanSetShutter_ReturnsTrue()
    {
        Assert.True(_dome.CanSetShutter);
    }

    [Fact]
    public void CanFindHome_ReturnsFalse()
    {
        Assert.False(_dome.CanFindHome);
    }

    [Fact]
    public void CanPark_ReturnsFalse()
    {
        Assert.False(_dome.CanPark);
    }

    #endregion
}

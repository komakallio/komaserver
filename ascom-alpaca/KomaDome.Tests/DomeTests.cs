using ASCOM.Common.DeviceInterfaces;
using KomaAlpacaCommon;
using KomaDome.DomeRestApi;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KomaDome.Tests;

public class DomeTests : IDisposable
{
    private readonly Mock<IDomeApi> _api;
    private readonly Dome _dome;

    public DomeTests()
    {
        var factory = new Mock<IRefitClientFactory<IDomeApi>>();
        _api = new Mock<IDomeApi>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_api.Object);
        _dome = new Dome(factory.Object, Options.Create<DomeOptions>(new() { BaseUrl = "http://invalid.invalid" }));
    }

    public void Dispose()
    {
        _dome.Dispose();
    }

    private void SetupStatusResponse(string state)
    {
        _api.Setup(a => a.GetStatusAsync()).ReturnsAsync(new RoofStatus { State = state });
    }

    #region Connection

    [Fact]
    public void Connect_WithOpenRoof_SetsConnectedAndShutterOpen()
    {
        SetupStatusResponse("OPEN");

        _dome.Connected = true;

        Assert.True(_dome.Connected);
        Assert.False(_dome.Connecting);
        Assert.Equal(ShutterState.Open, _dome.ShutterStatus);
    }

    [Fact]
    public void Connect_WithClosedRoof_SetsShutterClosed()
    {
        SetupStatusResponse("CLOSED");

        _dome.Connected = true;

        Assert.Equal(ShutterState.Closed, _dome.ShutterStatus);
    }

    [Fact]
    public void Disconnect_AfterConnect_SetsConnectedFalse()
    {
        SetupStatusResponse("OPEN");
        _dome.Connected = true;

        _dome.Connected = false;

        Assert.False(_dome.Connected);
        Assert.False(_dome.Connecting);
    }

    [Fact]
    public void Connect_WhenAlreadyConnected_DoesNotCallApiAgain()
    {
        SetupStatusResponse("OPEN");
        _dome.Connected = true;

        _dome.Connected = true;

        // Only one call from the first connect
        _api.Verify(a => a.GetStatusAsync(), Times.Once);
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

        _dome.Connected = true;

        Assert.Equal(expected, _dome.ShutterStatus);
    }

    #endregion

    #region Shutter control

    [Fact]
    public void OpenShutter_CallsApiAndSetsOpening()
    {
        SetupStatusResponse("CLOSED");
        _dome.Connected = true;

        _dome.OpenShutter();

        _api.Verify(a => a.OpenAsync(), Times.Once);
        Assert.Equal(ShutterState.Opening, _dome.ShutterStatus);
    }

    [Fact]
    public void CloseShutter_CallsApiAndSetsClosing()
    {
        SetupStatusResponse("OPEN");
        _dome.Connected = true;

        _dome.CloseShutter();

        _api.Verify(a => a.CloseAsync(), Times.Once);
        Assert.Equal(ShutterState.Closing, _dome.ShutterStatus);
    }

    [Fact]
    public void AbortSlew_CallsStopOnApi()
    {
        SetupStatusResponse("OPENING");
        _dome.Connected = true;

        _dome.AbortSlew();

        _api.Verify(a => a.StopAsync(), Times.Once);
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

        _dome.Connected = true;

        Assert.Equal(expectedSlewing, _dome.Slewing);
    }

    #endregion

    #region DeviceState

    [Fact]
    public void DeviceState_WhenConnected_ReturnsValues()
    {
        SetupStatusResponse("OPEN");
        _dome.Connected = true;

        var state = _dome.DeviceState;

        Assert.NotEmpty(state);
        Assert.Contains(state, s => s.Name == "ShutterStatus");
        Assert.Contains(state, s => s.Name == "Slewing");
        Assert.Contains(state, s => s.Name == "TimeStamp");
    }

    [Fact]
    public void DeviceState_WhenNotConnected_ReturnsEmpty()
    {
        var state = _dome.DeviceState;

        Assert.Empty(state);
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

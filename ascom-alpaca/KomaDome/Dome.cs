using ASCOM;
using ASCOM.Common.DeviceInterfaces;
using KomaAlpacaCommon;
using KomaDome.DomeRestApi;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace KomaDome;

public class Dome(IRefitClientFactory<IDomeApi> refitClientFactory, IOptions<DomeOptions> options, string user) : IDomeV3
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));
    private CancellationTokenSource _cancellationTokenSource = new();
    private Task? _timerTask;

    private ShutterState _shutterStatus = ShutterState.Error;

    #region Basic information

    public string Description => "Dome driver for Komakallio observatory";

    public string DriverInfo => "Alpaca driver for Komakallio dome/roof";

    public string DriverVersion => "1.0";

    public short InterfaceVersion => 3;

    public string Name => "Komakallio Dome";

    #endregion

    public ShutterState ShutterStatus => _shutterStatus;

    public bool Slewing => _shutterStatus is ShutterState.Opening or ShutterState.Closing;

    public bool CanSetShutter => true;

    public bool CanFindHome => false;

    public bool CanPark => false;

    public bool CanSetAltitude => false;

    public bool CanSetAzimuth => false;

    public bool CanSetPark => false;

    public bool CanSlave => false;

    public bool CanSyncAzimuth => false;

    public double Altitude => throw new PropertyNotImplementedException();

    public bool AtHome => throw new PropertyNotImplementedException();

    public bool AtPark => throw new PropertyNotImplementedException();

    public double Azimuth => throw new PropertyNotImplementedException();

    public bool Slaved
    {
        get => throw new PropertyNotImplementedException();
        set => throw new PropertyNotImplementedException();
    }

    private bool _connected = false;

    public bool Connected
    {
        get => _connected;
        set
        {
            try
            {
                if (value)
                {
                    ConnectAsync().Wait();
                }
                else
                {
                    DisconnectAsync().Wait();
                }
            }
            catch (Exception ex)
            {
                throw new DriverException("Failed to connect to Komakallio dome", ex);
            }
        }
    }

    public bool Connecting { get; internal set; }

    public List<StateValue> DeviceState => Connected ? [
        new StateValue(nameof(ShutterStatus), ShutterStatus.ToString()),
        new StateValue(nameof(Slewing), Slewing),
        new StateValue("TimeStamp", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
    ] : [];

    #region Unused legacy

    public IList<string> SupportedActions => [];

    public string Action(string ActionName, string ActionParameters)
    {
        throw new MethodNotImplementedException();
    }

    public void CommandBlind(string Command, bool Raw = false)
    {
        throw new MethodNotImplementedException();
    }

    public bool CommandBool(string Command, bool Raw = false)
    {
        throw new MethodNotImplementedException();
    }

    public string CommandString(string Command, bool Raw = false)
    {
        throw new MethodNotImplementedException();
    }

    #endregion

    public void Connect()
    {
        _ = ConnectAsync();
    }

    public void Disconnect()
    {
        _ = DisconnectAsync();
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _timer.Dispose();
    }

    private async Task ConnectAsync()
    {
        if (Connected || Connecting)
        {
            return;
        }

        Connecting = true;
        try
        {
            var status = await CreateApiClient().GetStatusAsync(user);
            _shutterStatus = ParseShutterState(status.State);
            _timerTask = StartPollingLoop();
            _connected = true;
        }
        catch (Exception)
        {
            // TODO: Log error
        }
        finally
        {
            Connecting = false;
        }
    }

    private async Task DisconnectAsync()
    {
        if (!Connected)
        {
            return;
        }

        Connecting = true;
        await StopPollingAsync();
        _connected = false;
        Connecting = false;
    }

    private async Task StartPollingLoop()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                var status = await CreateApiClient().GetStatusAsync(user);
                _shutterStatus = ParseShutterState(status.State);
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was stopped, exit the loop
        }
    }

    private async Task StopPollingAsync()
    {
        if (_timerTask is null)
        {
            return;
        }

        await _cancellationTokenSource.CancelAsync();
        await _timerTask;
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();
    }

    private static ShutterState ParseShutterState(string state) => state switch
    {
        "OPEN" => ShutterState.Open,
        "CLOSED" => ShutterState.Closed,
        "OPENING" => ShutterState.Opening,
        "CLOSING" => ShutterState.Closing,
        _ => ShutterState.Error,
    };

    public void OpenShutter()
    {
        CreateApiClient().OpenAsync(user).Wait();
        _shutterStatus = ShutterState.Opening;
    }

    public void CloseShutter()
    {
        CreateApiClient().CloseAsync(user).Wait();
        _shutterStatus = ShutterState.Closing;
    }

    public void AbortSlew()
    {
        CreateApiClient().StopAsync(user).Wait();
    }

    public void FindHome()
    {
        throw new MethodNotImplementedException();
    }

    public void Park()
    {
        throw new MethodNotImplementedException();
    }

    public void SetPark()
    {
        throw new MethodNotImplementedException();
    }

    public void SlewToAltitude(double Altitude)
    {
        throw new MethodNotImplementedException();
    }

    public void SlewToAzimuth(double Azimuth)
    {
        throw new MethodNotImplementedException();
    }

    public void SyncToAzimuth(double Azimuth)
    {
        throw new MethodNotImplementedException();
    }

    private IDomeApi CreateApiClient()
    {
        return refitClientFactory.CreateClient(options.Value.BaseUrl);
    }
}

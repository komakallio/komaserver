using ASCOM;
using ASCOM.Common.DeviceInterfaces;
using KomaDome.DomeRestApi;
using System.Globalization;

namespace KomaDome;

public class Dome(IDomeApi api, string user) : IDomeV3
{
    #region Basic information

    public string Description => "Dome driver for Komakallio observatory";

    public string DriverInfo => "Alpaca driver for Komakallio dome/roof";

    public string DriverVersion => "1.0";

    public short InterfaceVersion => 3;

    public string Name => "Komakallio Dome";

    #endregion

    public ShutterState ShutterStatus
    {
        get
        {
            var status = api.GetStatusAsync(user).GetAwaiter().GetResult();
            return ParseShutterState(status.State);
        }
    }

    public bool Slewing => ShutterStatus is ShutterState.Opening or ShutterState.Closing;

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
        get => false;
        set => throw new PropertyNotImplementedException();
    }

    /// <summary>
    /// Connected is always true, as this driver is not connected to any physical device and thus cannot be disconnected.
    /// </summary>
    public bool Connected
    {
        get => true;
        set
        {
        }
    }

    public bool Connecting => false;

    public List<StateValue> DeviceState => Connected ? [
        new StateValue(nameof(ShutterStatus), ShutterStatus),
        new StateValue(nameof(Slewing), Slewing),
        new StateValue("TimeStamp", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
    ] : [];

    public void Connect()
    {
    }

    public void Disconnect()
    {
    }

    public void Dispose()
    {
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
        api.OpenAsync(user).Wait();
    }

    public void CloseShutter()
    {
        api.CloseAsync(user).Wait();
    }

    public void AbortSlew()
    {
        api.StopAsync(user).Wait();
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

}

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

    #region Connect/disconnect

    public bool Connected
    {
        get => true;
        set { }
    }

    public bool Connecting => false;

    public void Connect()
    {
    }

    public void Disconnect()
    {
    }

    #endregion

    #region Capabilities

    public bool CanSetShutter => true;

    public bool CanFindHome => false;

    public bool CanPark => false;

    public bool CanSetAltitude => false;

    public bool CanSetAzimuth => false;

    public bool CanSetPark => false;

    public bool CanSlave => false;

    public bool CanSyncAzimuth => false;

    #endregion

    public double Altitude => throw new ASCOM.PropertyNotImplementedException();

    public bool AtHome => throw new ASCOM.PropertyNotImplementedException();

    public bool AtPark => throw new ASCOM.PropertyNotImplementedException();

    public double Azimuth => throw new ASCOM.PropertyNotImplementedException();

    public bool Slaved
    {
        get => false;
        set => throw new ASCOM.PropertyNotImplementedException();
    }

    public ShutterState ShutterStatus
    {
        get
        {
            try
            {
                var status = api.GetStatusAsync(user).GetAwaiter().GetResult();
                return ParseShutterState(status.State);
            }
            catch (Exception ex)
            {
                throw new ASCOM.DriverException("Error fetching shutter status", ex);
            }
        }
    }

    public bool Slewing => ShutterStatus is ShutterState.Opening or ShutterState.Closing;

    public List<StateValue> DeviceState => Connected ? [
        new StateValue(nameof(ShutterStatus), ShutterStatus),
        new StateValue(nameof(Slewing), Slewing),
        new StateValue("TimeStamp", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
    ] : [];

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
        try
        {
            api.OpenAsync(user).Wait();
        }
        catch (Exception ex)
        {
            throw new ASCOM.DriverException("Failed to open shutter", ex);
        }
    }

    public void CloseShutter()
    {
        try
        {
            api.CloseAsync(user).Wait();
        }
        catch (Exception ex)
        {
            throw new ASCOM.DriverException("Failed to close shutter", ex);
        }
    }

    public void AbortSlew()
    {
        try
        {
            api.StopAsync(user).Wait();
        }
        catch (Exception ex)
        {
            throw new ASCOM.DriverException("Failed to stop dome movement", ex);
        }
    }

    public void FindHome()
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void Park()
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void SetPark()
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void SlewToAltitude(double Altitude)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void SlewToAzimuth(double Azimuth)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void SyncToAzimuth(double Azimuth)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void Dispose()
    {
    }

    #region Unused legacy

    public IList<string> SupportedActions => [];

    public string Action(string ActionName, string ActionParameters)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void CommandBlind(string Command, bool Raw = false)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public bool CommandBool(string Command, bool Raw = false)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public string CommandString(string Command, bool Raw = false)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    #endregion

}

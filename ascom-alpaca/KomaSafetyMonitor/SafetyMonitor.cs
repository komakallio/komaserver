using ASCOM.Common.DeviceInterfaces;
using KomaSafetyMonitor.SafetyRestApi;
using System.Globalization;

namespace KomaSafetyMonitor;

public class SafetyMonitor(ISafetyStatusSource safetyStatusSource) : ISafetyMonitorV3
{
    #region Basic information

    public string Description => "Safety monitor for Komakallio observatory";

    public string DriverInfo => "Alpaca driver for Komakallio safety monitor";

    public string DriverVersion => "1.0";

    public short InterfaceVersion => 3;

    public string Name => "Komakallio Safety Monitor";

    #endregion

    public bool IsSafe
    {
        get
        {
            // TODO: Log if something goes wrong
            var result = safetyStatusSource.GetStatusAsync().GetAwaiter().GetResult();
            return result.Value is not null && ParseSafetyStatus(result.Value);
        }
    }

    public List<StateValue> DeviceState
    {
        get
        {
            if (!Connected)
                return [];

            var result = safetyStatusSource.GetStatusAsync().GetAwaiter().GetResult();
            var isSafe = result.Value is not null && ParseSafetyStatus(result.Value);

            return [
                new StateValue("IsSafe", isSafe ? 1 : 0),
                new StateValue("TimeStamp", result.FetchedAt.ToString("o", CultureInfo.InvariantCulture)),
            ];
        }
    }

    /// <summary>
    /// Connected is always true, as this driver is not connected to any physical device and thus cannot be disconnected.
    /// The same Alpaca device will also be available to several consumers on the network, and they should not be able to
    /// globally connect or disconnect the device.
    /// </summary>
    public bool Connected
    {
        get
        {
            return true;
        }
        set
        {
        }
    }

    public bool Connecting => false;

    public void Connect()
    {
    }

    public void Disconnect()
    {
    }

    public void Dispose()
    {
    }

    private static bool ParseSafetyStatus(SafetyStatus status)
    {
        // TODO: Check status details if configured so
        return status.Safe;
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

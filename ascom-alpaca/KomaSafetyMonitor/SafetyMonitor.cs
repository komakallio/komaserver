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
            var result = FetchSafetyStatus();
            return ParseSafetyStatus(result.Value);
        }
    }

    public List<StateValue> DeviceState
    {
        get
        {
            if (!Connected)
                return [];

            var result = FetchSafetyStatus();
            var isSafe = ParseSafetyStatus(result.Value);

            return [
                new StateValue("IsSafe", isSafe),
                new StateValue("TimeStamp", result.FetchedAt.ToString("o", CultureInfo.InvariantCulture)),
            ];
        }
    }

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

    private static bool ParseSafetyStatus(SafetyStatus status)
    {
        // TODO: Check status details if configured so
        return status.Safe;
    }

    private TimestampedResult<SafetyStatus> FetchSafetyStatus()
    {
        var result = safetyStatusSource.GetStatusAsync().GetAwaiter().GetResult();
        return result ?? throw new ASCOM.DriverException("Failed to fetch safety status");
    }
}

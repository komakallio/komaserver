using ASCOM.Common.DeviceInterfaces;

namespace KomaAlpacaServer.DeviceAccess
{
    public class SafetyMonitor : ISafetyMonitorV3
    {
        public bool IsSafe { get; }

        public bool Connected { get; set; } = false;

        public string Description => "Safety monitor for Komakallio observatory";

        public string DriverInfo => "Alpaca driver for Komakallio safety monitor";

        public string DriverVersion => "1.0";

        public short InterfaceVersion => 3;

        public string Name => "Komakallio Safety Monitor";

        public IList<string> SupportedActions => [];

        public bool Connecting => throw new NotImplementedException();

        public List<StateValue> DeviceState => throw new NotImplementedException();

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

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new ASCOM.NotImplementedException();
        }
    }
}

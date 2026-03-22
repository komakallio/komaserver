using ASCOM;
using ASCOM.Common.DeviceInterfaces;
using KomaSafetyMonitor.SafetyRestApi;
using System.Globalization;

namespace KomaSafetyMonitor
{
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
                if (!_connected)
                    return false;

                var status = safetyStatusSource.GetStatusAsync().GetAwaiter().GetResult();
                return status is not null && ParseSafetyStatus(status);
            }
        }

        private bool _connected = false;
        /// <summary>
        /// As of ASCOM Platform 7, the setter of Connected should not be used.
        /// The asynchronous Connect and Disconnect methods should be used instead,
        /// which will update the Connecting and Connected properties accordingly.
        /// </summary>
        public bool Connected
        {
            get
            {
                return _connected;
            }
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
                    throw new DriverException("Failed to connect to Komakallio safety monitor", ex);
                }
            }
        }

        public bool Connecting { get; internal set; }

        public List<StateValue> DeviceState => Connected ? [
            new StateValue("IsSafe", IsSafe ? 1 : 0),
            new StateValue("TimeStamp", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
        ] : [];

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
                await safetyStatusSource.GetStatusAsync();
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

        private Task DisconnectAsync()
        {
            if (!Connected)
            {
                return Task.CompletedTask;
            }

            _connected = false;
            return Task.CompletedTask;
        }

        private static bool ParseSafetyStatus(SafetyStatus status)
        {
            // TODO: Check status details if configured so
            return status.Safe;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using System.Linq;
using System.Timers;

namespace ASCOM.Komakallio
{
    /// <summary>
    /// ASCOM SafetyMonitor Driver for Komakallio.
    /// </summary>
    [Guid("be987b5a-31ab-4f18-8dda-861a5a307d5d")]
    [ClassInterface(ClassInterfaceType.None)]
    public class SafetyMonitor : ISafetyMonitor
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal const string DriverID = "ASCOM.Komakallio.SafetyMonitor";

        /// <summary>
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private const string DriverDescription = "Komakallio SafetyMonitor Driver";

        // Constants used for Profile persistence
        private const string ServerAddressProfileName = "Server Address";
        private const string ServerAddressDefault = "http://192.168.0.110:9002/safety";
        private const string FiltersSubKey = "Filters";

        internal static string ServerAddress { get; set; }
        internal static List<Filter> Filters { get; set; } = new List<Filter>();

        // Data
        private int mErrorCount = 0;
        private DateTime mLastUpdate;
        private System.Timers.Timer mUpdateTimer;

        /// <summary>
        /// Private variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
        /// </summary>
        private TraceLogger mLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Komakallio"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public SafetyMonitor()
        {
            
            mLogger = new TraceLogger("", "Komakallio");

#if DEBUG
            // Enable logging in debug mode
            mLogger.Enabled = true;
#endif

            // Read device configuration from the ASCOM Profile store
            ReadProfile();
        }


        //
        // PUBLIC COM INTERFACE ISafetyMonitor IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (mConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");

            using (SetupDialogForm F = new SetupDialogForm())
            {
                var result = F.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    WriteProfile(); // Persist device configuration values to the ASCOM Profile store
                }
            }
        }

        public ArrayList SupportedActions
        {
            get
            {
                mLogger.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
        }

        public void CommandBlind(string command, bool raw)
        {
            CheckConnected("CommandBlind");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public bool CommandBool(string command, bool raw)
        {
            CheckConnected("CommandBool");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public string CommandString(string command, bool raw)
        {
            CheckConnected("CommandString");
            throw new ASCOM.MethodNotImplementedException("CommandBlind");
        }

        public void Dispose()
        {
            if (Connected)
                Connected = false;
            // Clean up the tracelogger and util objects
            mLogger.Enabled = false;
            mLogger.Dispose();
            mLogger = null;
        }


        private bool mConnected = false;
        public bool Connected
        {
            get
            {
                mLogger.LogMessage("Connected Get", mConnected.ToString());
                return mConnected;
            }
            set
            {
                mLogger.LogMessage("Connected Set", value.ToString());
                if (mConnected == value)
                    return;

                if (value)
                {
                    mConnected = true;
                    LogMessage("Connected Set", "Connecting to server {0}", ServerAddress);

                    UpdateSafetyMonitorData(null, null);

                    if (mUpdateTimer == null)
                    {
                        mUpdateTimer = new System.Timers.Timer(10 * 1000);
                        mUpdateTimer.Elapsed += UpdateSafetyMonitorData;
                        mUpdateTimer.AutoReset = true;
                        mUpdateTimer.Start();
                    }
                }
                else
                {
                    mConnected = false;
                    LogMessage("Connected Set", "Disconnecting from server {0}", ServerAddress);

                    if (mUpdateTimer != null)
                    {
                        mUpdateTimer.Dispose();
                        mUpdateTimer = null;
                    }
                }
            }
        }

        public string Description
        {
            get
            {
                mLogger.LogMessage("Description Get", DriverDescription);
                return DriverDescription;
            }
        }

        public string DriverInfo
        {
            get
            {
                mLogger.LogMessage("DriverInfo Get", "Finding driver information...");
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                mLogger.LogMessage("DriverInfo Get", driverInfo);
                return driverInfo;
            }
        }

        public string DriverVersion
        {
            get
            {
                mLogger.LogMessage("DriverVersion Get", "Finding driver version...");
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                mLogger.LogMessage("DriverVersion Get", driverVersion);
                return driverVersion;
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                mLogger.LogMessage("InterfaceVersion Get", "1");
                return Convert.ToInt16("1");
            }
        }

        public string Name
        {
            get
            {
                string name = "KomaSafetyMonitor";
                mLogger.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ISafetyMonitor Implementation
        public bool IsSafe { get; private set; } = false;

        #endregion

        #region Private methods

        private void UpdateSafetyMonitorData(Object source, ElapsedEventArgs e)
        {
            try
            {
                var status = new SafetyServer(ServerAddress).Status;

                IsSafe = status.IsSafeWithFilters(Filters);

                mLastUpdate = DateTime.Now;
                mErrorCount = 0;
                mLogger.LogMessage("UpdateSafetyMonitorData", "Received safety status: " + IsSafe);
            } catch(Exception except) {
                mLogger.LogMessage("UpdateSafetyMonitorData", "Error" + except.Message);
                if (++mErrorCount > 5)
                {
                    mLogger.LogMessage("UpdateSafetyMonitorData", "Too many communication errors, declaring system unsafe");
                    IsSafe = false;
                }
            }
        }

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered.
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregASCOM(bool bRegister)
        {
            using (var P = new ASCOM.Utilities.Profile())
            {
                P.DeviceType = "SafetyMonitor";
                if (bRegister)
                {
                    P.Register(DriverID, DriverDescription);
                }
                else
                {
                    P.Unregister(DriverID);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            RegUnregASCOM(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            RegUnregASCOM(false);
        }

        #endregion

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!mConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal void ReadProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                ServerAddress = driverProfile.GetValue(DriverID, ServerAddressProfileName, string.Empty, ServerAddressDefault);

                Filters.Clear();
                ArrayList filterValues;
                try
                {
                    filterValues = driverProfile.Values(DriverID, FiltersSubKey);
                } catch (NullReferenceException) {
                    filterValues = new ArrayList();
                }

                foreach (KeyValuePair filterValue in filterValues)
                {
                    Filters.Add(new Filter()
                    {
                        Name = filterValue.Key,
                        Active = bool.Parse(filterValue.Value)
                    });
                }
            }
        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal void WriteProfile()
        {
            using (Profile driverProfile = new Profile())
            {
                driverProfile.DeviceType = "SafetyMonitor";
                driverProfile.WriteValue(DriverID, ServerAddressProfileName, ServerAddress.ToString());

                driverProfile.DeleteSubKey(DriverID, FiltersSubKey);
                foreach (var filter in Filters)
                {
                    driverProfile.WriteValue(DriverID, filter.Name, filter.Active.ToString(), FiltersSubKey);
                }
            }
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        internal void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            mLogger.LogMessage(identifier, msg);
        }

        #endregion
    }
}

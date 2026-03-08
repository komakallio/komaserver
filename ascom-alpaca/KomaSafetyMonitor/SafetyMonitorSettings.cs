using KomaAlpacaCommon;

namespace KomaSafetyMonitor;

public class SafetyMonitorSettings
{
    private static readonly ASCOM.Tools.XMLProfile Profile = new(Constants.DriverID, "safetymonitor");

    public static void Reset()
    {
        Profile.Clear();
    }

    public static string BaseUrl
    {
        get
        {
            return Profile.GetValue("BaseUrl", "http://192.168.1.8:9002");
        }
        set
        {
            Profile.WriteValue("BaseUrl", value.ToString());
        }
    }
}

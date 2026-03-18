using KomaAlpacaCommon;

namespace KomaDome;

public class DomeSettings
{
    private static readonly ASCOM.Tools.XMLProfile Profile = new(Constants.DriverID, "dome");

    public static void Reset()
    {
        Profile.Clear();
    }

    public static string BaseUrl
    {
        get => Profile.GetValue("BaseUrl", "http://192.168.1.8:9000/roof/USER");
        set => Profile.WriteValue("BaseUrl", value.TrimEnd('/'));
    }
}

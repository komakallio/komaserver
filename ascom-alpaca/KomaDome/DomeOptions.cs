using System.Text.RegularExpressions;

namespace KomaDome;

public partial class DomeOptions
{
    public required string BaseUrl { get; set; }
    public required string[] Users { get; set; }

    public static string ToApiUser(string friendlyName) =>
        ApiUserRegex().Replace(friendlyName.ToLowerInvariant(), "");

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex ApiUserRegex();
}

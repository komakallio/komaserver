using ASCOM.Common.DeviceInterfaces;
using KomaObservingConditions.WeatherRestApi;
using System.Globalization;

namespace KomaObservingConditions;

public class ObservingConditions(IWeatherStatusSource weatherStatusSource) : IObservingConditionsV2
{
    #region Basic information
    public string Description => "Observing conditions for Komakallio observatory";

    public string DriverInfo => "Alpaca driver for Komakallio observing conditions";

    public string DriverVersion => "1.0";

    public short InterfaceVersion => 2;

    public string Name => "Komakallio Observing Conditions";

    #endregion

    public List<StateValue> DeviceState
    {
        get
        {
            var result = FetchWeatherStatus();
            return
            [
                new(nameof(Temperature), result.Value.Temperature),
                new(nameof(Humidity), result.Value.Humidity),
                new(nameof(Pressure), result.Value.Pressure),
                new(nameof(WindSpeed), result.Value.WindSpeed),
                new(nameof(WindGust), result.Value.WindGust),
                new(nameof(WindDirection), result.Value.WindDir),
                new(nameof(RainRate), result.Value.RainRate),
                new(nameof(DewPoint), result.Value.DewPoint),
                new("TimeStamp", result.FetchedAt.ToString("o", CultureInfo.InvariantCulture)),
            ];
        }
    }

    public double AveragePeriod
    {
        get => 0.0;
        set
        {
            if (value != 0.0)
            {
                throw new ASCOM.DriverException("Only instantaneous values are supported");
            }
        }
    }

    public double Temperature => FetchWeatherStatus().Value.Temperature;

    public double DewPoint => FetchWeatherStatus().Value.DewPoint;

    public double Humidity => FetchWeatherStatus().Value.Humidity;

    public double Pressure => FetchWeatherStatus().Value.Pressure;

    public double RainRate => FetchWeatherStatus().Value.RainRate;

    public double WindDirection => FetchWeatherStatus().Value.WindDir;

    public double WindGust => FetchWeatherStatus().Value.WindGust;

    public double WindSpeed => FetchWeatherStatus().Value.WindSpeed;

    public double CloudCover => throw new ASCOM.PropertyNotImplementedException();

    public double SkyBrightness => throw new ASCOM.PropertyNotImplementedException();

    public double SkyQuality => throw new ASCOM.PropertyNotImplementedException();

    public double StarFWHM => throw new ASCOM.PropertyNotImplementedException();

    public double SkyTemperature => throw new ASCOM.PropertyNotImplementedException();

    public bool Connected { get => true; set { } }

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

    public void Refresh()
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public string SensorDescription(string PropertyName)
    {
        ValidateSupportedSensorName(PropertyName);
        return "Komakallio weather station";
    }

    public double TimeSinceLastUpdate(string PropertyName)
    {
        if (!string.IsNullOrEmpty(PropertyName))
            ValidateSupportedSensorName(PropertyName);

        var result = FetchWeatherStatus();
        return (DateTime.UtcNow - result.FetchedAt).TotalSeconds;
    }

    #region Unused legacy

    public IList<string> SupportedActions => [];

    public string Action(string actionName, string actionParameters)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public void CommandBlind(string command, bool raw = false)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public bool CommandBool(string command, bool raw = false)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    public string CommandString(string command, bool raw = false)
    {
        throw new ASCOM.MethodNotImplementedException();
    }

    #endregion

    private static readonly string[] SupportedSensors =
    [
        nameof(Temperature),
        nameof(DewPoint),
        nameof(Humidity),
        nameof(Pressure),
        nameof(RainRate),
        nameof(WindDirection),
        nameof(WindGust),
        nameof(WindSpeed),
    ];

    private static readonly string[] AllSensors =
    [
        nameof(Temperature),
        nameof(DewPoint),
        nameof(Humidity),
        nameof(Pressure),
        nameof(RainRate),
        nameof(WindDirection),
        nameof(WindGust),
        nameof(WindSpeed),
        nameof(CloudCover),
        nameof(SkyBrightness),
        nameof(SkyQuality),
        nameof(StarFWHM),
        nameof(SkyTemperature),
    ];

    private static void ValidateSupportedSensorName(string propertyName)
    {
        if (!AllSensors.Any(s => s.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
            throw new ASCOM.InvalidValueException($"No such sensor in Alpaca spec: {propertyName}");

        if (!SupportedSensors.Any(s => s.Equals(propertyName, StringComparison.OrdinalIgnoreCase)))
            throw new ASCOM.MethodNotImplementedException($"No such sensor implemented: {propertyName}");
    }

    private TimestampedResult<WeatherStatus> FetchWeatherStatus()
    {
        var result = weatherStatusSource.GetStatusAsync().GetAwaiter().GetResult();
        return result ?? throw new ASCOM.DriverException("Failed to fetch weather status");
    }
}

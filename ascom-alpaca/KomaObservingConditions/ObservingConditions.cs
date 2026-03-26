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
            var weatherStatus = weatherStatusSource.GetStatusAsync()
                                                   .GetAwaiter()
                                                   .GetResult() ?? throw new ASCOM.DriverException("Failed to get weather status");
            return
            [
                new(nameof(Temperature), weatherStatus.Temperature),
                new(nameof(Humidity), weatherStatus.Humidity),
                new(nameof(Pressure), weatherStatus.Pressure),
                new(nameof(WindSpeed), weatherStatus.WindSpeed),
                new(nameof(WindGust), weatherStatus.WindGust),
                new(nameof(WindDirection), weatherStatus.WindDir),
                new(nameof(RainRate), weatherStatus.RainRate),
                new(nameof(DewPoint), weatherStatus.DewPoint),
                new("TimeStamp", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
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

    public double Temperature => GetWeatherStatus().Temperature;

    public double DewPoint => GetWeatherStatus().DewPoint;

    public double Humidity => GetWeatherStatus().Humidity;

    public double Pressure => GetWeatherStatus().Pressure;

    public double RainRate => GetWeatherStatus().RainRate;

    public double WindDirection => GetWeatherStatus().WindDir;

    public double WindGust => GetWeatherStatus().WindGust;

    public double WindSpeed => GetWeatherStatus().WindSpeed;

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
        var supportedSensors = new[]
        {
            nameof(Temperature),
            nameof(DewPoint),
            nameof(Humidity),
            nameof(Pressure),
            nameof(RainRate),
            nameof(WindDirection),
            nameof(WindGust),
            nameof(WindSpeed),
        }.Select(s => s.ToLowerInvariant());

        if (supportedSensors.Contains(PropertyName.ToLowerInvariant()))
        {
            return "Komakallio weather station";
        }

        throw new ASCOM.MethodNotImplementedException($"No such sensor: {PropertyName}");
    }

    public double TimeSinceLastUpdate(string PropertyName)
    {
        var supportedSensors = new[]
        {
            nameof(Temperature),
            nameof(DewPoint),
            nameof(Humidity),
            nameof(Pressure),
            nameof(RainRate),
            nameof(WindDirection),
            nameof(WindGust),
            nameof(WindSpeed),
        }.Select(s => s.ToLowerInvariant());

        if (supportedSensors.Contains(PropertyName.ToLowerInvariant()))
        {
            return 0.0;
        }

        throw new ASCOM.MethodNotImplementedException($"No such sensor: {PropertyName}");
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

    private WeatherStatus GetWeatherStatus() => weatherStatusSource.GetStatusAsync()
                                                                   .GetAwaiter()
                                                                   .GetResult() ?? throw new ASCOM.DriverException("Failed to get weather status");
}

using KomaObservingConditions.WeatherRestApi;

namespace KomaObservingConditions;

public interface IWeatherStatusSource
{
    Task<WeatherStatus?> GetStatusAsync();
}
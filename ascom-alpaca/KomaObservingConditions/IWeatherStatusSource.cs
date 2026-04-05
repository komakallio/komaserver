using KomaObservingConditions.WeatherRestApi;

namespace KomaObservingConditions;

public interface IWeatherStatusSource
{
    Task<TimestampedResult<WeatherStatus>> GetStatusAsync();
}
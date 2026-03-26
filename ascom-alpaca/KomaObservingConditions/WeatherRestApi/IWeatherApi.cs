using Refit;

namespace KomaObservingConditions.WeatherRestApi;

internal interface IWeatherApi
{
    [Get("/api/weather")]
    Task<WeatherStatus> GetWeatherStatusAsync();
}

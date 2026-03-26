using KomaObservingConditions.WeatherRestApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace KomaObservingConditions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObservingConditions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddRefitClient<IWeatherApi>().ConfigureHttpClient(c =>
        {
            var options = new ObservingConditionsOptions();
            configuration.GetSection(nameof(ObservingConditionsOptions)).Bind(options);
            c.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        });
        services.AddSingleton<IWeatherStatusSource, WeatherStatusCache>();
        services.AddSingleton<ObservingConditions>();

        return services;
    }
}

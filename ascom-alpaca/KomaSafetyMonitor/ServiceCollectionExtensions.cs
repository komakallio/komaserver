using KomaSafetyMonitor.SafetyRestApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace KomaSafetyMonitor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSafetyMonitor(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddRefitClient<ISafetyMonitorApi>().ConfigureHttpClient(c =>
        {
            var options = new SafetyMonitorOptions
            {
                BaseUrl = ""
            };
            configuration.GetSection(nameof(SafetyMonitorOptions)).Bind(options);
            c.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        });
        services.AddSingleton<ISafetyStatusSource, SafetyStatusCache>();
        services.AddSingleton<SafetyMonitor>();

        return services;
    }
}

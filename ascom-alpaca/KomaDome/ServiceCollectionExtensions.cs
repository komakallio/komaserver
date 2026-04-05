using KomaDome.DomeRestApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace KomaDome;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDome(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DomeOptions>(configuration.GetSection(nameof(DomeOptions)));
        services.AddRefitClient<IDomeApi>().ConfigureHttpClient(c =>
        {
            var options = new DomeOptions
            {
                BaseUrl = "",
                Users = [],
            };
            configuration.GetSection(nameof(DomeOptions)).Bind(options);
            c.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        });

        return services;
    }
}

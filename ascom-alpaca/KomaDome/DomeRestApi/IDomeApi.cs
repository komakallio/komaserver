using Refit;

namespace KomaDome.DomeRestApi;

public interface IDomeApi
{
    [Get("/")]
    Task<RoofStatus> GetStatusAsync();

    [Post("/open")]
    Task OpenAsync();

    [Post("/close")]
    Task CloseAsync();

    [Post("/stop")]
    Task StopAsync();
}

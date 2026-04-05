using Refit;

namespace KomaDome.DomeRestApi;

public interface IDomeApi
{
    [Get("/roof/{user}")]
    Task<RoofStatus> GetStatusAsync(string user);

    [Post("/roof/{user}/open")]
    Task OpenAsync(string user);

    [Post("/roof/{user}/close")]
    Task CloseAsync(string user);

    [Post("/roof/{user}/stop")]
    Task StopAsync(string user);
}

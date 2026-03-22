using Refit;

namespace KomaDome.DomeRestApi;

public interface IDomeApi
{
    [Get("/{user}/")]
    Task<RoofStatus> GetStatusAsync(string user);

    [Post("/{user}/open")]
    Task OpenAsync(string user);

    [Post("/{user}/close")]
    Task CloseAsync(string user);

    [Post("/{user}/stop")]
    Task StopAsync(string user);
}

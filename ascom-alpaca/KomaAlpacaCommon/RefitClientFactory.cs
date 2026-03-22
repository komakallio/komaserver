namespace KomaAlpacaCommon;

/// <summary>
/// This allows creating API clients using Refit so that the
/// base URL can be changed at runtime.
/// </summary>
/// <typeparam name="T">API interface class</typeparam>
/// <param name="httpClientFactory">HTTP client factory for creating new HTTP clients with the given base address</param>
public class RefitClientFactory<T>(IHttpClientFactory httpClientFactory) : IRefitClientFactory<T>
{
    public T CreateClient(string baseAddress)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(baseAddress.TrimEnd('/'));
        return Refit.RestService.For<T>(httpClient);
    }
}

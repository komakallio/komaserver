namespace KomaAlpacaCommon;

public interface IRefitClientFactory<T>
{
    T CreateClient(string baseAddress);
}
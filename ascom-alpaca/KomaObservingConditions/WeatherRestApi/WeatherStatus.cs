namespace KomaObservingConditions.WeatherRestApi;

public class WeatherStatus
{
    public double Temperature { get; init; }
    public double Humidity { get; init; }
    public double Pressure { get; init; }
    public double WindSpeed { get; init; }
    public double WindGust { get; init; }
    public double WindDir { get; init; }
    public double RainRate { get; init; }
    public double DewPoint { get; init; }
}
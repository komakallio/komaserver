var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/weather", () =>
{
    return new
    {
        temperature = 0.3,
        humidity = 83.9,
        pressure = 1010.3,
        windspeed = 0.3,
        windgust = 0.4,
        winddir = 337,
        rainrate = 0,
        dewpoint = -2.1,
    };
});

app.Run();


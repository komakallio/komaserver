var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/safety", () =>
{
    return new
    {
        safe = true,
        details = new
        {
            temperature = new
            {
                value = 0.2,
                safe = true,
            },
            rainintensity = new
            {
                value = 0,
                safe = true,
            },
            raintrigger = new
            {
                value = 0,
                safe = true,
            },
            rainradar10km = new
            {
                value = 0,
                safe = true,
            },
            rainradar30km = new
            {
                value = 0,
                safe = true,
            },
            rainradar50km = new
            {
                value = 0,
                safe = true,
            },
            sunaltitude = new
            {
                value = -24.88,
                safe = true,
            },
            moonaltitude = new
            {
                value = 43.35,
                safe = true,
            },
            upscharge = new
            {
                value = 100,
                safe = true,
            },
            enclosuretemp = new
            {
                value = 2.75,
                safe = true,
            },
        },
    };
});

app.Run();

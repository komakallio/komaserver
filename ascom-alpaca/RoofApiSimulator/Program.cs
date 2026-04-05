using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var roofs = new ConcurrentDictionary<string, UserRoofState>();

UserRoofState GetRoof(string user) => roofs.GetOrAdd(user, _ => new UserRoofState());

app.MapGet("/roof/{user}", (ILogger<Program> logger, string user) =>
{
    var roof = GetRoof(user);
    lock (roof)
    {
        var reportedState = roof.State switch
        {
            "OPENING" or "CLOSING" or "STOPPING" or "STOPPED" or "ERROR" => roof.State,
            _ => roof.IsOpen ? "OPEN" : "CLOSED"
        };
        logger.LogInformation("Roof status requested for {user}, status is: {status}", user, reportedState);
        return Results.Json(new { state = reportedState, open = roof.IsOpen });
    }
});

app.MapPost("/roof/{user}/open", (ILogger<Program> logger, string user) =>
{
    var roof = GetRoof(user);
    logger.LogInformation("Roof opening requested for {user}, current state is {state}", user, roof.State);
    lock (roof)
    {
        switch (roof.State)
        {
            case "OPEN":
                roof.IsOpen = true;
                break;
            case "OPENING":
                roof.PendingOpen = true;
                break;
            case "STOPPED":
            case "CLOSED":
                roof.PendingOpen = true;
                roof.StartTransition("OPENING", "OPEN");
                break;
            case "CLOSING":
                break;
            case "ERROR":
                return Results.Json(new { message = "ROOF IN ERROR", error = true }, statusCode: 400);
        }
        return Results.Json(new { message = "OK" });
    }
});

app.MapPost("/roof/{user}/close", (ILogger<Program> logger, string user) =>
{
    var roof = GetRoof(user);
    logger.LogInformation("Roof closing requested for {user}, current state is {state}", user, roof.State);
    lock (roof)
    {
        switch (roof.State)
        {
            case "STOPPED":
            case "OPEN":
                roof.IsOpen = false;
                roof.StartTransition("CLOSING", "CLOSED");
                break;
            case "OPENING":
                roof.IsOpen = false;
                roof.PendingOpen = false;
                break;
            case "CLOSED":
            case "CLOSING":
                break;
            case "ERROR":
                return Results.Json(new { message = "ROOF IN ERROR", error = true }, statusCode: 400);
        }
        return Results.Json(new { message = "OK" });
    }
});

app.MapPost("/roof/{user}/stop", (ILogger<Program> logger, string user) =>
{
    var roof = GetRoof(user);
    logger.LogInformation("Roof stopping requested for {user}, current state is {state}", user, roof.State);
    lock (roof)
    {
        roof.CancelTransition();
        roof.State = "STOPPED";
        roof.IsOpen = false;
        roof.PendingOpen = false;
    }
    return Results.Json(new { message = "OK" });
});

app.Run();

class UserRoofState
{
    private static readonly TimeSpan TransitionDuration = TimeSpan.FromSeconds(10);

    private CancellationTokenSource? _cts;

    public string State { get; set; } = "CLOSED";
    public bool IsOpen { get; set; }
    public bool PendingOpen { get; set; }

    public void StartTransition(string transitioning, string target)
    {
        CancelTransition();
        State = transitioning;
        var cts = new CancellationTokenSource();
        _cts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TransitionDuration, cts.Token);
                lock (this)
                {
                    if (cts.Token.IsCancellationRequested)
                        return;

                    State = target;
                    _cts = null;

                    if (target == "OPEN")
                    {
                        if (PendingOpen)
                            IsOpen = true;
                        PendingOpen = false;
                    }
                    else if (target == "CLOSED")
                    {
                        IsOpen = false;
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    public void CancelTransition()
    {
        _cts?.Cancel();
        _cts = null;
    }
}

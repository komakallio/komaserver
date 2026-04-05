# Komakallio ASCOM Alpaca Server

ASCOM Alpaca server for the Komakallio observatory. Wraps three remote observatory REST APIs (dome/roof control, safety monitoring, weather) and exposes them as standard [ASCOM Alpaca](https://ascom-standards.org/Developer/Alpaca.htm) device drivers.

Built with ASP.NET Core Blazor on .NET 8.0.

## Devices

| Alpaca Device          | Upstream API       | Description                                      |
|------------------------|--------------------|--------------------------------------------------|
| Dome (x3)             | Roof control API   | One instance per pier (east, center, west)       |
| SafetyMonitor         | Safety monitor API | Observatory safety status                        |
| ObservingConditions   | Weather API        | Temperature, humidity, pressure, wind, rain, etc. |

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build and Run

```bash
dotnet build
dotnet run --project KomaAlpacaServer
```

The server starts on port 12345 by default.

### Running with Simulators

Three API simulators are included for local development (roof, safety monitor, weather). Use the **"With simulators"** launch configuration to start them all alongside the server.

### Tests

```bash
dotnet test
```

## Solution Structure

```
KomaAlpacaServer/              # ASP.NET Core Blazor web app (Alpaca server)
KomaDome/                      # Dome device driver library
KomaSafetyMonitor/             # SafetyMonitor device driver library
KomaObservingConditions/       # ObservingConditions device driver library
KomaDome.Tests/                # Tests for Dome driver
KomaSafetyMonitor.Tests/       # Tests for SafetyMonitor driver
KomaObservingConditions.Tests/ # Tests for ObservingConditions driver
RoofApiSimulator/              # Roof API simulator
SafetyMonitorApiSimulator/     # Safety monitor API simulator
WeatherApiSimulator/           # Weather API simulator
```

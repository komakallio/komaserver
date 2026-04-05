# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASCOM Alpaca server for the Komakallio observatory. Wraps three remote observatory REST APIs (dome/roof control, safety monitoring, weather) and exposes them as standard ASCOM Alpaca device drivers. Built with ASP.NET Core Blazor (.NET 10.0).

## Build & Test Commands

```bash
dotnet build                                       # Build all projects
dotnet test                                        # Run all tests
dotnet test --filter FullyQualifiedName~DomeTests  # Run a specific test class
dotnet run --project KomaAlpacaServer              # Run the server (default port 12345)
```

The server accepts CLI flags: `--reset` (reset settings), `--reset-auth` (reset auth), `--urls=http://localhost:PORT` (custom port).

## API Simulators

Three minimal ASP.NET Core apps simulate the upstream REST APIs for local development:
- **RoofApiSimulator** - simulates roof open/close/stop with per-user state and timed transitions
- **SafetyMonitorApiSimulator** - returns hardcoded safe status with detail values
- **WeatherApiSimulator** - returns hardcoded weather data (temperature, humidity, pressure, wind, rain, dewpoint)

Use the **"With simulators"** launch configuration to run all three simulators alongside the Alpaca server.

## Architecture

**Solution structure:** One ASP.NET Core Blazor web app (`KomaAlpacaServer`) references three device driver libraries (`KomaDome`, `KomaSafetyMonitor`, `KomaObservingConditions`). Each driver library has a corresponding test project (except ObservingConditions).

**Device driver pattern:** Each driver library follows the same structure:
- A main class implementing an ASCOM interface (`IDomeV3`, `ISafetyMonitorV3`, `IObservingConditionsV2`)
- A Refit-generated HTTP client interface for the upstream REST API
- An options class for configuration (base URL, etc.)
- A `ServiceCollectionExtensions` class for DI registration
- All drivers report `Connected = true` always — they are stateless network proxies

**Caching:** SafetyMonitor and ObservingConditions use a cache class with 5-second expiry, `SemaphoreSlim`-based stampede protection, and double-check locking. Dome does not cache.

**Multi-instance devices:** The Dome driver is instantiated once per pier user (eastpier, centerpier, westpier), each registered as a separate Alpaca device.

**Configuration:** Server settings are persisted via ASCOM XML Profile (`ServerSettings.cs`). Device endpoint URLs come from `appsettings.{Environment}.json`.

## Testing

xUnit + Moq. Tests mock the Refit API interface and verify ASCOM interface behavior. Each driver library has its own test project (convention: `{ProjectName}.Tests`).

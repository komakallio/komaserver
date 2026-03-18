# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Komakallio ASCOM Alpaca Server — a C# .NET 8.0 ASP.NET Core Blazor Server application exposing observatory devices over the ASCOM Alpaca REST protocol. Built for the Komakallio observatory.

## Build & Run

```bash
# Build the solution
dotnet build KomaAlpacaServer.slnx

# Run the server
dotnet run --project KomaAlpacaServer/KomaAlpacaServer.csproj

# Run on a specific port
dotnet run --project KomaAlpacaServer/KomaAlpacaServer.csproj -- --urls=http://localhost:12345
```

There are no automated tests in this project.

The server starts at `http://localhost:5077` in development (via launchSettings.json), defaulting to port 12345 in production. The web UI includes a Setup page and per-device configuration pages.

Useful startup flags: `--reset` (clear all settings), `--reset-auth` (disable auth to allow password change), `--local-address` (print local IP/port).

## Architecture

Three projects:

- **KomaAlpacaServer** — ASP.NET Core Blazor Server host. `Program.cs` configures the ASCOM Alpaca framework, registers devices with `DeviceManager`, and sets up auth/Swagger. `ServerSettings.cs` manages persistent config via ASCOM `XMLProfile`. Razor pages in `Pages/` provide the setup UI.
- **KomaSafetyMonitor** — Implements `ISafetyMonitorV3`. Polls an external REST API every 3 seconds via a `PeriodicTimer` and a Refit client. Safety state is derived from a `SafetyStatus` record with 10 sensor fields.
- **KomaAlpacaCommon** — Shared utilities: `IRefitClientFactory`/`RefitClientFactory` for creating Refit HTTP clients with runtime-configurable base URLs.

### Key patterns

**Adding a new device:** Implement the relevant ASCOM interface (e.g., `ISafetyMonitorV3`), register it in `Program.cs` via `DeviceManager`, and create a setup Razor page under `Pages/Devices/`. Store device-specific settings in a dedicated settings class using `XMLProfile` (see `SafetyMonitorSettings.cs` as the model).

**Runtime-configurable URLs:** Use `IRefitClientFactory` to create Refit clients. The factory allows changing base URLs at runtime without restart — the client is recreated on each configuration change.

**Persistent settings:** `ServerSettings` and device-specific settings classes use ASCOM `XMLProfile` for storage. Each setting has an explicit key constant and default value.

**External API integration:** Define a Refit interface (see `ISafetyMonitorApi`), register it via `IRefitClientFactory`, and inject into the device implementation.

## NuGet Sources

`KomaAlpacaServer/NuGet.config` includes a MyGet feed for ASCOM Initiative packages (`ASCOM.Alpaca.Razor`, `ASCOM.Common.Components`, `ASCOM.Tools`).

## External Safety Monitor API

The safety monitor polls `GET /safety` at the configured base URL (default `http://192.168.1.8:9002`). It expects a `SafetyStatus` JSON response with a `Safe` boolean and a `Details` object containing 10 `SafetyValue` fields (each with `Value` and `Safe`): temperature, rain intensity, rain trigger, rain radar at 3 km/10 km/30 km, sun altitude, moon altitude, UPS charge, and enclosure temperature.

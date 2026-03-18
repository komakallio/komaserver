# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Komakallio ASCOM Dome Driver — a .NET Framework 4.0 COM interop DLL that exposes the observatory roof as an ASCOM `IDomeV2` device. It communicates with a backend HTTP REST API to open, close, and poll the roof status.

The sibling directory `../ascom-alpaca/` contains a newer ASCOM Alpaca server (see its own CLAUDE.md). The current git branch `alpaca-driver` reflects ongoing work toward that transition.

## Build

Open `KomaDome.sln` in Visual Studio 2013+ and build, or use MSBuild:

```bash
msbuild KomaDome.sln /p:Configuration=Release
```

Both Debug and Release configurations have `RegisterForComInterop=true`, so a build on Windows will register the COM component. The output assembly is `ASCOM.Komakallio.Dome.dll`.

There are no automated tests. Manual testing requires installing the ASCOM Platform (6.2+) and connecting via the ASCOM Chooser.

## Architecture

**Single project**: `KomaDome/` — one C# class library targeting .NET 4.0 Client Profile.

### Driver.cs

The core of the driver. Implements `IDomeV2` (ASCOM standard dome interface) as a COM-visible class (`ASCOM.Komakallio.Dome`).

**HTTP communication**: Uses `HttpWebRequest` to talk to a REST API at a configurable address (default `http://192.168.0.110:9000/roof/USER`). Three endpoints:
- `POST /open` — open the roof
- `POST /close` — close the roof
- `POST /stop` — abort movement
- `GET <serverAddress>` — poll status (returns JSON with a `state` field)

**State polling**: A `System.Threading.Timer` fires `UpdateRoofData` every 5 seconds while connected. The timer starts on `Connected = true` and stops on `Connected = false`.

**State mapping**: The API returns string states (`OPEN`, `CLOSED`, `OPENING`, `CLOSING`) which are mapped to `ShutterState` enum values. Unknown states map to `shutterError`.

**Unimplemented**: Altitude/azimuth control, Park, FindHome, and slaving all throw `ASCOM.MethodNotImplementedException`. Only shutter open/close is functional.

**Configuration**: Server address is persisted via `ASCOM.Utilities.Profile` (Windows registry under the ASCOM key). `ReadProfile()` / `WriteProfile()` manage this.

**Logging**: `TraceLogger` writes to an ASCOM diagnostic log. Enabled only in Debug builds (`#if DEBUG`).

### SetupDialogForm.cs

A Windows Forms dialog that lets users change the server address. Opened by astronomy software via `SetupDialog()`. Reads/writes `serverAddress` static field and calls `WriteProfile()`.

### Installer

`KomaDome/KomakallioRoof Setup.iss` is an Inno Setup script that packages the DLL and registers it via `regasm.exe` for both 32-bit and 64-bit. Requires ASCOM Platform 6.2 to be pre-installed.

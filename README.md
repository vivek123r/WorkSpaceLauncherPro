# WorkSpace Launcher Pro

A professional Windows desktop application that restores complete work environments with a single click.

> **Status:** In development. See `.commandcode/plans/workspace-launcher-pro.md` for the full implementation plan.

## Features

- **Profile system** — unlimited named profiles (Job Mode, Android Dev, Web Dev, AI Research, etc.)
- **Smart Layout Suggestions** — count-aware layout picker (2..8+ windows) with live drag-to-swap
- **Window position engine** — DPI-aware, multi-monitor, Win11 shadow-correct
- **Visual Workspace Designer** — drag-and-drop monitor canvas
- **Browser automation** — Chrome, Edge, Opera GX, Brave with profile directories
- **PWA launcher** — WhatsApp, Claude, ChatGPT, Notion via AUMID
- **System tray + Windows startup + global hotkeys**
- **PowerShell script import** — converts the user's existing `.ps1` launchers into profiles
- **JSON import/export** for profile sharing across PCs

## Tech stack

- C# / .NET 8 / WPF
- Win32 / DWM / User32 P/Invoke
- SQLite (Microsoft.Data.Sqlite + Dapper)
- MVVM (CommunityToolkit.Mvvm 8.4.0)
- DI (Microsoft.Extensions.DependencyInjection 8.0.1)
- Self-contained single-file EXE, no installer

## Build

Prerequisites: **.NET 8 SDK** (download from <https://aka.ms/dotnet/download>).

```powershell
dotnet build -c Debug
dotnet run --project src/WorkSpaceLauncherPro.App
```

## Publish (single-file EXE)

```powershell
dotnet publish src/WorkSpaceLauncherPro.App -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:PublishReadyToRun=true `
  --self-contained true -o publish
```

Output: `publish/WorkSpaceLauncherPro.App.exe` (~70–90 MB self-contained).

## Project structure

```
src/
  WorkSpaceLauncherPro.App/        WPF shell, MVVM, Views
  WorkSpaceLauncherPro.Core/       Win32, P/Invoke, MonitorEnum, WindowPosition, PWA, Browser
  WorkSpaceLauncherPro.Data/       SQLite + Dapper repositories
  WorkSpaceLauncherPro.Layout/     Smart Layout engine, LiveSwap
tests/
  WorkSpaceLauncherPro.Tests/      xUnit + FluentAssertions
tools/
  PowerShellFixtures/              Copies of the user's real .ps1 scripts for tests
  Publish.ps1                      Repeatable single-file publish
```

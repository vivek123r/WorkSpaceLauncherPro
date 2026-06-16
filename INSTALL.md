# Installing the .NET 8 SDK

The project requires the **.NET 8 SDK** to build. The .NET 8 **runtime** is already on your
machine (`C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.24`), but the SDK
(MSBuild, project system, templates, `dotnet build`) is not.

## Method 1 — winget (recommended, one command)

Open **PowerShell** or **Windows Terminal** and run:

```powershell
winget install Microsoft.DotNet.SDK.8
```

That's it. The installer is silent and takes ~1-2 minutes.

After it finishes, **close and reopen your terminal** (so PATH updates) and verify:

```powershell
dotnet --list-sdks
```

You should see something like:

```
8.0.100 [C:\Program Files\dotnet\sdk]
8.0.404 [C:\Program Files\dotnet\sdk]   ← if a patch version is current
```

> **Note:** if `winget` isn't installed, run `Add-AppxPackage -Path "https://github.com/microsoft/winget-cli/releases/latest/download/Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"` first, or use Method 2.

## Method 2 — Microsoft official installer

1. Go to <https://dotnet.microsoft.com/download/dotnet/8.0>
2. Under **SDK 8.0.x → Windows**, click **x64** (or arm64 if you have an ARM Surface)
3. Run the downloaded `dotnet-sdk-8.0.xxx-win-x64.exe`
4. Follow the installer (next-next-finish)
5. **Close and reopen** your terminal

## Method 3 — PowerShell install script

If neither of the above work, this one-liner does the same thing as Method 2:

```powershell
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "$env:TEMP\dotnet-install.ps1"
& "$env:TEMP\dotnet-install.ps1" -Channel 8.0 -InstallDir "C:\Program Files\dotnet" -NoPath
```

This installs to the same `dotnet` folder your runtime already lives in, so it doesn't disturb anything.

---

## Verifying the install

In a **new** PowerShell window:

```powershell
dotnet --version        # should print 8.0.x
dotnet --list-sdks      # should show 8.0.x
dotnet new list wpf     # should list WPF templates
```

If you see templates listed, you're good.

---

## Building WorkSpace Launcher Pro

Once the SDK is installed:

```powershell
cd C:\Users\vivek\projects\WorkSpaceLauncherPro
dotnet build -c Debug
```

You should see a clean build with **0 errors**. Warnings about PowerShell fixtures or
deprecated XAML are OK.

Run the app:

```powershell
dotnet run --project src\WorkSpaceLauncherPro.App
```

A window titled **"🚀 WorkSpace Launcher Pro"** should open with the dark Mica theme.

Run the tests:

```powershell
dotnet test
```

Should print: `Passed: 28, Failed: 0`.

Publish a single-file EXE:

```powershell
dotnet publish src\WorkSpaceLauncherPro.App -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:PublishReadyToRun=true `
  --self-contained true -o publish
```

Output: `publish\WorkSpaceLauncherPro.App.exe` (~70-90 MB self-contained, no install needed).

---

## Troubleshooting

### "dotnet: command not found" after install
Your terminal has the old PATH cached. **Close and reopen it**. If it still fails, run
`$env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine")` in the current window.

### "No .NET SDKs were found" but `dotnet --version` works
You installed only the runtime, not the SDK. Run `winget install Microsoft.DotNet.SDK.8` again.

### Build error MSB4019: "The imported project ... Microsoft.NET.Sdk.WindowsDesktop.targets was not found"
You have the wrong TFM. Make sure `<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>`
is in the csproj and that you installed the .NET 8 SDK (not 7 or 9).

### WPF / WinForms errors like "The type 'Application' is not defined"
You need `<UseWPF>true</UseWPF>` and `<UseWindowsForms>true</UseWindowsForms>` in the csproj
for the project that uses WPF/WinForms. The WorkSpace Launcher Pro csprojs already have these.

### Build succeeds but app crashes on launch
Check `%LOCALAPPDATA%\WorkSpaceLauncherPro\logs\` for the error. Most common cause:
SQLite native libs not bundled — make sure `SQLitePCLRaw.bundle_e_sqlite3` is referenced
(it's in the Data csproj already).

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Launching;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;
using WorkSpaceLauncherPro.Layout;

namespace WorkSpaceLauncherPro.App.Services;

public sealed class LauncherService : ILauncherService
{
    private readonly WindowEnumerator _enumerator;
    private readonly WindowPositionEngine _engine;
    private readonly IMonitorEnumerator _monitors;
    private readonly PwaLauncher _pwa;
    private readonly BrowserLauncher _browser;
    private readonly LayoutEngine _layoutEngine;
    private readonly ILogger<LauncherService> _log;

    public LauncherService(
        WindowEnumerator enumerator,
        WindowPositionEngine engine,
        IMonitorEnumerator monitors,
        PwaLauncher pwa,
        BrowserLauncher browser,
        LayoutEngine layoutEngine,
        ILogger<LauncherService> log)
    {
        _enumerator = enumerator;
        _engine = engine;
        _monitors = monitors;
        _pwa = pwa;
        _browser = browser;
        _layoutEngine = layoutEngine;
        _log = log;
    }

    public Task<LaunchReport> LaunchWithDefaultLayoutAsync(Profile profile, CancellationToken ct = default)
    {
        var monitors = _monitors.Current();
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        if (primary is null) return Task.FromResult(new LaunchReport(0, 0, 0, TimeSpan.Zero));
        var layout = _layoutEngine.DefaultFor(profile.Apps.Count, primary);
        return LaunchWithLayoutAsync(profile, layout, ct);
    }

    public async Task<LaunchReport> LaunchWithLayoutAsync(Profile profile, LayoutTemplate layout, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var monitors = _monitors.Current();
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        if (primary is null)
        {
            _log.LogWarning("No monitors detected — aborting launch");
            return new LaunchReport(0, 0, profile.Apps.Count, sw.Elapsed);
        }

        var layoutRects = layout.Generate(profile.Apps.Count, primary);
        _log.LogInformation("Launching {Count} apps with layout '{Layout}'",
            profile.Apps.Count, layout.Name);

        using var sem = new SemaphoreSlim(4);
        var launchTasks = profile.Apps.Select(async (app, i) =>
        {
            await sem.WaitAsync(ct);
            try
            {
                if (app.LaunchDelayMs > 0) await Task.Delay(app.LaunchDelayMs, ct);
                var launched = LaunchOne(app);
                return new { App = app, Index = i, Launched = launched };
            }
            finally { sem.Release(); }
        }).ToList();

        var launched = await Task.WhenAll(launchTasks);

        int placed = 0, failed = 0;
        foreach (var item in launched)
        {
            if (item.Launched is null) { failed++; continue; }
            var title = item.App.MatchRule?.TitleContains;
            var hwnd = _enumerator.FindByPid(item.Launched.Pid, title, TimeSpan.FromSeconds(12), ct);
            if (hwnd is null || hwnd == IntPtr.Zero)
            {
                _log.LogWarning("No window appeared for {App} (pid {Pid})",
                    item.App.DisplayName, item.Launched.Pid);
                failed++;
                continue;
            }

            var rect = item.App.Placement is { } p
                ? p
                : new ProfileAppPlacement(
                    layoutRects[item.Index].MonitorIndex,
                    layoutRects[item.Index].X,
                    layoutRects[item.Index].Y,
                    layoutRects[item.Index].Width,
                    layoutRects[item.Index].Height);

            var result = await _engine.ApplyAsync(hwnd.Value, rect, monitors, ct);
            if (result.Success) placed++;
            else failed++;
        }

        sw.Stop();
        return new LaunchReport(profile.Apps.Count, placed, failed, sw.Elapsed);
    }

    private LaunchedApp? LaunchOne(ProfileApp app)
    {
        return app.Kind switch
        {
            AppTargetKind.Aumid => _pwa.LaunchByAumid(app.Target, app.LaunchArgs),
            AppTargetKind.Executable => LaunchExe(app),
            AppTargetKind.Url => LaunchUrl(app),
            AppTargetKind.BrowserProfile => LaunchBrowserProfile(app),
            AppTargetKind.ShellFolder => LaunchShellFolder(app),
            _ => null
        };
    }

    private LaunchedApp? LaunchExe(ProfileApp app)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = app.Target,
                Arguments = app.LaunchArgs ?? "",
                UseShellExecute = true
            };
            var p = Process.Start(psi);
            return p is null ? null : new LaunchedApp(app.Target, (uint)p.Id, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "LaunchExe failed for {Target}", app.Target);
            return null;
        }
    }

    private LaunchedApp? LaunchUrl(ProfileApp app)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(app.Target) { UseShellExecute = true });
            return p is null ? null : new LaunchedApp(app.Target, (uint)p.Id, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "LaunchUrl failed for {Target}", app.Target);
            return null;
        }
    }

    private LaunchedApp? LaunchBrowserProfile(ProfileApp app)
    {
        BrowserLauncher.BrowserInstall? browser = app.Target.ToLowerInvariant() switch
        {
            "chrome" => _browser.DetectChrome(),
            "edge" => _browser.DetectEdge(),
            "brave" => _browser.DetectBrave(),
            "operagx" or "opera-gx" or "opera" => _browser.DetectOperaGx(),
            _ => null
        };
        if (browser is null || string.IsNullOrEmpty(app.BrowserProfileDir)) return null;
        return _browser.LaunchWithProfile(browser, app.BrowserProfileDir, app.LaunchArgs);
    }

    private LaunchedApp? LaunchShellFolder(ProfileApp app)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo("explorer.exe", $"\"{app.Target}\"") { UseShellExecute = true });
            return p is null ? null : new LaunchedApp(app.Target, (uint)p.Id, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "LaunchShellFolder failed for {Target}", app.Target);
            return null;
        }
    }
}

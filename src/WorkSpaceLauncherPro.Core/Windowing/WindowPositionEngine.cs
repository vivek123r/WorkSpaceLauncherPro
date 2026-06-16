using System.Runtime.InteropServices;
using CoreModels = WorkSpaceLauncherPro.Core.Models;
using Microsoft.Extensions.Logging;

namespace WorkSpaceLauncherPro.Core.Windowing;

public sealed class PlacementResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public static PlacementResult Ok() => new() { Success = true };
    public static PlacementResult Fail(string error) => new() { Success = false, Error = error };
}

public sealed class WindowPositionEngine
{
    private readonly ILogger<WindowPositionEngine> _log;

    public WindowPositionEngine(ILogger<WindowPositionEngine> log) => _log = log;

    /// <summary>
    /// Apply a placement to a window handle. Idempotent — safe to call multiple times
    /// (the LiveSwap controller calls it twice per swap, once per window).
    /// </summary>
    public async Task<PlacementResult> ApplyAsync(
        IntPtr hwnd,
        CoreModels.ProfileAppPlacement target,
        IReadOnlyList<MonitorInfo> monitors,
        CancellationToken ct = default)
    {
        if (hwnd == IntPtr.Zero) return PlacementResult.Fail("Invalid hwnd");

        // 1. Resolve target monitor (stable by index, fallback to primary)
        var mon = monitors.FirstOrDefault(m => m.Index == target.MonitorIndex)
                  ?? monitors.FirstOrDefault(m => m.IsPrimary)
                  ?? monitors.FirstOrDefault();
        if (mon is null) return PlacementResult.Fail("No monitors available");

        // 2. Compute absolute physical rect
        var absX = mon.BoundsPhysical.Left + target.X;
        var absY = mon.BoundsPhysical.Top + target.Y;
        var w = Math.Max(100, target.Width);
        var h = Math.Max(100, target.Height);

        try
        {
            // 3. Restore from minimized/maximized first
            User32.ShowWindow(hwnd, Win32.SW_RESTORE);
            await Task.Delay(20, ct);

            // 4. Read current placement
            var wp = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
            if (!User32.GetWindowPlacement(hwnd, ref wp))
            {
                _log.LogWarning("GetWindowPlacement failed: {Err}", Marshal.GetLastWin32Error());
            }

            // 5. Apply position. If currently maximized, use SetWindowPlacement to clear the
            //    maximized state atomically; otherwise use SetWindowPos.
            if (wp.showCmd == Win32.SW_SHOWMAXIMIZED)
            {
                wp.showCmd = Win32.SW_SHOWNORMAL;
                wp.flags = 0;
                wp.rcNormalPosition = new RECT
                {
                    Left = absX, Top = absY,
                    Right = absX + w, Bottom = absY + h
                };
                if (!User32.SetWindowPlacement(hwnd, ref wp))
                {
                    _log.LogWarning("SetWindowPlacement failed: {Err}", Marshal.GetLastWin32Error());
                }
            }
            else
            {
                // Win11 shadow compensation: -8 left when x>0, +8 right, +8 bottom, 0 top.
                // This matches the PowerShell prototype in job-mode.ps1.
                var sl = absX == 0 ? 0 : 8;
                var sr = 8;
                var sb = 8;
                if (!User32.SetWindowPos(hwnd, IntPtr.Zero,
                        absX - sl, absY,
                        w + sl + sr, h + sb,
                        Win32.SWP_NOACTIVATE | Win32.SWP_NOZORDER | Win32.SWP_FRAMECHANGED))
                {
                    _log.LogWarning("SetWindowPos failed: {Err}", Marshal.GetLastWin32Error());
                }
            }

            // 6. Mark the move complete so DWM doesn't snap back to "remembered" position
            User32.PostMessage(hwnd, Win32.WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);

            // 7. Apply final show state
            User32.ShowWindow(hwnd, target.ShowState switch
            {
                CoreModels.ShowState.Maximized => Win32.SW_SHOWMAXIMIZED,
                CoreModels.ShowState.Minimized => Win32.SW_SHOWMINIMIZED,
                _ => Win32.SW_SHOWNORMAL
            });

            return PlacementResult.Ok();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ApplyAsync failed for hwnd {Hwnd}", hwnd);
            return PlacementResult.Fail(ex.Message);
        }
    }

    /// <summary>Reads the DWM-extended (visible) bounds of a window. Useful for capture.</summary>
    public static RECT GetDwmBounds(IntPtr hwnd)
    {
        if (DwmApi.DwmGetWindowAttribute(hwnd, DwmApi.DWMWA_EXTENDED_FRAME_BOUNDS,
                out var r, Marshal.SizeOf<RECT>()) != 0)
        {
            User32.GetWindowRect(hwnd, out r);
        }
        return r;
    }
}

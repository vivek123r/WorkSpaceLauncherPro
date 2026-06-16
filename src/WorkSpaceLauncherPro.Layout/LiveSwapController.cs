using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Layout;

/// <summary>
/// Coordinates drag-to-swap interactions on a set of AppPlacements. Used by both
/// the live Magic Arrange overlay AND the static Visual Designer canvas — single
/// code path for both.
///
/// The "app placement" abstraction lives in the App layer (XAML canvas),
/// so this controller takes primitive rects and hwnds, not view models.
/// </summary>
public sealed class LiveSwapController
{
    private readonly WindowPositionEngine _engine;
    private readonly ILogger<LiveSwapController> _log;

    public LiveSwapController(WindowPositionEngine engine, ILogger<LiveSwapController> log)
    {
        _engine = engine;
        _log = log;
    }

    public event EventHandler<SwapEventArgs>? SwapCompleted;

    /// <summary>Swap two app placements. Animates both windows if both have live hwnds.</summary>
    public async Task OnSwapAsync(
        IntPtr? draggedHwnd, ProfileAppRect draggedRect,
        IntPtr? targetHwnd, ProfileAppRect targetRect,
        IReadOnlyList<MonitorInfo> monitors,
        CancellationToken ct = default)
    {
        // 1. Apply the new rects via the engine
        var draggedPlacement = new Core.Models.ProfileAppPlacement(
            draggedRect.MonitorIndex, draggedRect.X, draggedRect.Y, draggedRect.Width, draggedRect.Height);
        var targetPlacement = new Core.Models.ProfileAppPlacement(
            targetRect.MonitorIndex, targetRect.X, targetRect.Y, targetRect.Width, targetRect.Height);

        // 2. Animate if both have live windows
        if (draggedHwnd is { } dh && dh != IntPtr.Zero)
        {
            var from = WindowPositionEngine.GetDwmBounds(dh);
            await AnimateAsync(dh, from, draggedRect, TimeSpan.FromMilliseconds(220), ct);
            await _engine.ApplyAsync(dh, draggedPlacement, monitors, ct);
        }
        if (targetHwnd is { } th && th != IntPtr.Zero)
        {
            var from = WindowPositionEngine.GetDwmBounds(th);
            await AnimateAsync(th, from, targetRect, TimeSpan.FromMilliseconds(220), ct);
            await _engine.ApplyAsync(th, targetPlacement, monitors, ct);
        }

        SwapCompleted?.Invoke(this, new SwapEventArgs(draggedRect, targetRect));
        _log.LogInformation("Swapped app placements");
    }

    private static async Task AnimateAsync(IntPtr hwnd, RECT from, ProfileAppRect to, TimeSpan dur, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < dur)
        {
            if (ct.IsCancellationRequested) return;
            var t = (float)(sw.ElapsedMilliseconds / dur.TotalMilliseconds);
            t = Math.Clamp(t, 0f, 1f);
            // Ease-in-out
            t = t * t * (3f - 2f * t);
            int x = (int)Lerp(from.Left, to.X, t);
            int y = (int)Lerp(from.Top, to.Y, t);
            int w = (int)Lerp(from.Width, to.Width, t);
            int h = (int)Lerp(from.Height, to.Height, t);
            User32.SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h,
                Win32.SWP_NOACTIVATE | Win32.SWP_NOZORDER | Win32.SWP_FRAMECHANGED);
            await Task.Delay(16, ct);
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}

public sealed record ProfileAppRect(int MonitorIndex, int X, int Y, int Width, int Height);

public sealed class SwapEventArgs : EventArgs
{
    public ProfileAppRect Dragged { get; }
    public ProfileAppRect Target { get; }
    public SwapEventArgs(ProfileAppRect dragged, ProfileAppRect target)
    {
        Dragged = dragged; Target = target;
    }
}

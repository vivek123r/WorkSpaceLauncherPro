using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WorkSpaceLauncherPro.Core.Windowing;

public sealed class WindowEnumerator
{
    private readonly ILogger<WindowEnumerator> _log;

    public WindowEnumerator(ILogger<WindowEnumerator> log) => _log = log;

    /// <summary>
    /// Wait for a top-level, visible window belonging to the given PID.
    /// Returns the first match, or null on timeout. Polls every ~50ms.
    /// </summary>
    public IntPtr? FindByPid(uint pid, string? titleContains, TimeSpan timeout, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        IntPtr? found = null;
        while (sw.Elapsed < timeout)
        {
            if (ct.IsCancellationRequested) return null;

            found = EnumFirst(pid, titleContains);
            if (found is not null && IsReady(found.Value)) return found;
            Thread.Sleep(50);
        }
        return found;
    }

    /// <summary>Snapshot all visible top-level windows at this moment. Used as a "before" baseline
    /// so the launcher knows which windows it just spawned.</summary>
    public IReadOnlyList<(IntPtr Hwnd, uint Pid, string Title)> SnapshotOpenWindows()
    {
        var list = new List<(IntPtr, uint, string)>();
        User32.EnumWindows((h, _) =>
        {
            if (!User32.IsWindowVisible(h)) return true;
            User32.GetWindowThreadProcessId(h, out var pid);
            var sb = new StringBuilder(256);
            User32.GetWindowText(h, sb, sb.Capacity);
            list.Add((h, pid, sb.ToString()));
            return true;
        }, IntPtr.Zero);
        return list;
    }

    private static IntPtr? EnumFirst(uint pid, string? titleContains)
    {
        IntPtr? result = null;
        User32.EnumWindows((h, _) =>
        {
            User32.GetWindowThreadProcessId(h, out var p);
            if (p != pid) return true;
            if (!User32.IsWindowVisible(h)) return true;
            if (titleContains is { Length: > 0 })
            {
                var sb = new StringBuilder(256);
                User32.GetWindowText(h, sb, sb.Capacity);
                if (sb.ToString().IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) < 0)
                    return true;
            }
            result = h;
            return false; // stop enumeration
        }, IntPtr.Zero);
        return result;
    }

    private static bool IsReady(IntPtr h)
    {
        if (!User32.IsWindowVisible(h)) return false;
        if (User32.IsHungAppWindow(h)) return false;
        if (!User32.GetClientRect(h, out var r)) return false;
        if (r.Right - r.Left <= 0 || r.Bottom - r.Top <= 0) return false;
        // DWM extended frame should be retrievable
        var hr = DwmApi.DwmGetWindowAttribute(h, DwmApi.DWMWA_EXTENDED_FRAME_BOUNDS,
            out _, Marshal.SizeOf<RECT>());
        return hr == 0;
    }
}

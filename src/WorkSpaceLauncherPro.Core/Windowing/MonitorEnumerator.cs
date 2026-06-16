using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace WorkSpaceLauncherPro.Core.Windowing;

public interface IMonitorEnumerator
{
    IReadOnlyList<MonitorInfo> Current();
    MonitorInfo? FromDeviceName(string deviceName);
}

public sealed class MonitorEnumerator : IMonitorEnumerator
{
    private readonly DpiHelper _dpi;
    private readonly ILogger<MonitorEnumerator> _log;

    public MonitorEnumerator(DpiHelper dpi, ILogger<MonitorEnumerator> log)
    {
        _dpi = dpi;
        _log = log;
    }

    public IReadOnlyList<MonitorInfo> Current()
    {
        var list = new List<MonitorInfo>();
        int idx = 0;
        try
        {
            User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr h, IntPtr hdc, ref RECT r, IntPtr data) =>
                {
                    var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
                    if (!User32.GetMonitorInfo(h, ref mi)) return true;
                    var (dx, _) = _dpi.GetDpiForMonitor(h);
                    list.Add(new MonitorInfo(
                        Index: idx++,
                        Handle: h,
                        DeviceName: mi.szDevice ?? "",
                        BoundsPhysical: mi.rcMonitor,
                        WorkingAreaPhysical: mi.rcWork,
                        IsPrimary: (mi.dwFlags & Win32.MONITORINFOF_PRIMARY) != 0,
                        DpiScale: dx / 96.0));
                    return true;
                },
                IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "EnumDisplayMonitors failed");
        }
        // Stable ordering: primary first, then by device name
        return list
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.DeviceName, StringComparer.Ordinal)
            .Select((m, i) => m with { Index = i })
            .ToList();
    }

    public MonitorInfo? FromDeviceName(string deviceName)
    {
        var all = Current();
        return all.FirstOrDefault(m => string.Equals(m.DeviceName, deviceName, StringComparison.Ordinal));
    }
}

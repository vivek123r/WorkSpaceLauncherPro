using System.Runtime.InteropServices;

namespace WorkSpaceLauncherPro.Core.Windowing;

/// <summary>
/// Information about a single physical monitor. All bounds are in **physical pixels**.
/// DPI scale is the ratio of physical DPI to 96 (e.g. 1.5 for a 144 DPI display).
/// </summary>
public sealed record MonitorInfo(
    int Index,
    IntPtr Handle,
    string DeviceName,
    RECT BoundsPhysical,
    RECT WorkingAreaPhysical,
    bool IsPrimary,
    double DpiScale)
{
    public int Width => BoundsPhysical.Right - BoundsPhysical.Left;
    public int Height => BoundsPhysical.Bottom - BoundsPhysical.Top;
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left, Top, Right, Bottom;
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

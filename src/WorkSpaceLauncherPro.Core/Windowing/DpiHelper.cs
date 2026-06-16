namespace WorkSpaceLauncherPro.Core.Windowing;

public sealed class DpiHelper
{
    /// <summary>Returns the DPI scale for the given window (1.0 = 96 DPI, 1.5 = 144 DPI, etc.).</summary>
    public double GetScaleForWindow(IntPtr hwnd) => User32.GetDpiForWindow(hwnd) / 96.0;

    /// <summary>Returns the (X, Y) DPI for the given monitor handle.</summary>
    public (uint DpiX, uint DpiY) GetDpiForMonitor(IntPtr hMonitor)
    {
        var hr = ShCore.GetDpiForMonitor(hMonitor, Win32.MDT_EFFECTIVE_DPI, out var dx, out var dy);
        return hr == 0 ? (dx, dy) : (96u, 96u);
    }
}

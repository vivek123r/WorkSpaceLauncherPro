using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Core.Models;

/// <summary>A monitor-relative rectangle. The launcher engine converts this to absolute physical pixels.</summary>
public readonly record struct LayoutRect(int SlotIndex, int X, int Y, int Width, int Height, int MonitorIndex = 0);

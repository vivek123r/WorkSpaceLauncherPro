namespace WorkSpaceLauncherPro.Core.Models;

public enum AppTargetKind
{
    Executable = 0,
    Aumid = 1,                 // Windows Store / PWA app (e.g. WhatsApp, Claude PWA)
    Url = 2,                   // Default browser
    BrowserProfile = 3,        // Chromium browser with --profile-directory=
    ShellFolder = 4            // File Explorer on a path
}

public enum ShowState
{
    Normal = 1,
    Minimized = 2,
    Maximized = 3
}

public sealed class ProfileApp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "";
    public AppTargetKind Kind { get; set; }
    public string Target { get; set; } = "";          // exe path / AUMID / URL / folder
    public string? BrowserProfileDir { get; set; }     // only when Kind = BrowserProfile
    public string? LaunchArgs { get; set; }
    public WindowMatchRule? MatchRule { get; set; }    // null = match by PID
    public ProfileAppPlacement? Placement { get; set; } // null = use profile default layout
    public int LaunchDelayMs { get; set; }
    public int SortIndex { get; set; }
}

public sealed record WindowMatchRule(string? TitleContains, string? ProcessName);

/// <summary>
/// Per-app placement override. Coordinates are monitor-relative physical pixels
/// (DPI-stable per monitor — see WindowPositionEngine for conversion rules).
/// </summary>
public sealed record ProfileAppPlacement(
    int MonitorIndex,
    int X, int Y, int Width, int Height,
    ShowState ShowState = ShowState.Normal);

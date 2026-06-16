using MediaColor = System.Windows.Media.Color;

namespace WorkSpaceLauncherPro.Core.Models;

/// <summary>A named "mode" (e.g. Job Mode, Android Dev) — a collection of apps to launch together.</summary>
public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Profile";
    public string IconGlyph { get; set; } = "🚀";
    public MediaColor AccentColor { get; set; } = MediaColor.FromRgb(0x1A, 0x6C, 0xF5);
    public Hotkey Hotkey { get; set; } = new();
    public string? DefaultLayout { get; set; }
    public string? TargetMonitorDevice { get; set; }
    public int SortIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ProfileApp> Apps { get; set; } = new();
}

public readonly record struct Hotkey(int Modifiers, int VirtualKey)
{
    public static readonly Hotkey None = new();
    public bool IsSet => Modifiers != 0 && VirtualKey != 0;
}

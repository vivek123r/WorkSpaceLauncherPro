using System.Windows;

namespace WorkSpaceLauncherPro.App.Services;

/// <summary>
/// Registers global Windows hotkeys (Win32 RegisterHotKey) and forwards
/// activations to a per-profile Launch action. Wired in M5.
/// </summary>
public interface IHotkeyService : IDisposable
{
    void Start(Window messageWindow);
}

public sealed class HotkeyService : IHotkeyService
{
    public void Start(Window messageWindow)
    {
        // Implementation deferred to M5:
        // - HwndSource.AddHook on the message window
        // - RegisterHotKey for per-profile hotkeys + Ctrl+Alt+Space quick picker
        // - On WM_HOTKEY, look up the profile and call ILauncherService
    }

    public void Dispose() { }
}

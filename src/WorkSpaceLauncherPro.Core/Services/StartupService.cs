using Microsoft.Win32;

namespace WorkSpaceLauncherPro.Core.Services;

/// <summary>
/// Toggles "Launch on Windows startup" by writing/reading the
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run registry value.
/// </summary>
public sealed class StartupService
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WorkSpaceLauncherPro";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) is string s && s.Contains("WorkSpaceLauncherPro");
        }
    }

    public void Enable(string exePath, string args = "--tray")
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
        var quoted = string.IsNullOrEmpty(args) ? $"\"{exePath}\"" : $"\"{exePath}\" {args}";
        key.SetValue(ValueName, quoted);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        if (key?.GetValue(ValueName) is not null)
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

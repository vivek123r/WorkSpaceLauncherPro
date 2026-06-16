using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WorkSpaceLauncherPro.Core.Launching;

/// <summary>
/// Resolves an AUMID to its install path / package full name using the local
/// registry (HKCU + HKLM). Lightweight, no WinRT dependency.
/// </summary>
public sealed class AppResolver
{
    private readonly ILogger<AppResolver> _log;

    public AppResolver(ILogger<AppResolver> log) => _log = log;

    public IReadOnlyList<InstalledApp> EnumerateInstalledPwas()
    {
        var list = new List<InstalledApp>();
        foreach (var hive in new[] { Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryHive.LocalMachine })
        {
            using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(hive,
                Environment.Is64BitOperatingSystem ? Microsoft.Win32.RegistryView.Registry64 : Microsoft.Win32.RegistryView.Registry32);
            using var aumidKey = baseKey.OpenSubKey(@"Software\Classes\AppUserModelId");
            if (aumidKey is null) continue;
            foreach (var sub in aumidKey.GetSubKeyNames())
            {
                using var k = aumidKey.OpenSubKey(sub);
                if (k is null) continue;
                var displayName = k.GetValue("DisplayName") as string;
                var icon = k.GetValue("DisplayIcon") as string;
                if (string.IsNullOrEmpty(displayName)) continue;
                list.Add(new InstalledApp(sub, displayName!, icon, null));
            }
        }
        return list;
    }

    public InstalledApp? FindByAumid(string aumid)
        => EnumerateInstalledPwas().FirstOrDefault(a => a.AumId == aumid);
}

public sealed record InstalledApp(string AumId, string DisplayName, string? Icon, string? InstallPath);

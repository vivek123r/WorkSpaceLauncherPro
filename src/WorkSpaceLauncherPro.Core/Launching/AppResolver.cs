using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace WorkSpaceLauncherPro.Core.Launching;

/// <summary>
/// Resolves an AUMID to its install path / package full name using the local
/// registry (HKCU + HKLM). Lightweight, no WinRT dependency.
///
/// Also enumerates installed Win32 apps by scanning Start Menu shortcuts
/// (per-user and common), and discovers well-known paths for browsers.
/// </summary>
public sealed class AppResolver
{
    private readonly ILogger<AppResolver> _log;

    public AppResolver(ILogger<AppResolver> log) => _log = log;

    /// <summary>UWP / PWA apps via the AppUserModelId registry hive.</summary>
    public IReadOnlyList<InstalledApp> EnumerateInstalledPwas()
    {
        var list = new List<InstalledApp>();
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
            using var aumidKey = baseKey.OpenSubKey(@"Software\Classes\AppUserModelId");
            if (aumidKey is null) continue;
            foreach (var sub in aumidKey.GetSubKeyNames())
            {
                using var k = aumidKey.OpenSubKey(sub);
                if (k is null) continue;
                var displayName = k.GetValue("DisplayName") as string;
                if (string.IsNullOrEmpty(displayName)) continue;
                list.Add(new InstalledApp(sub, displayName, null, null, AppSourceKind.Uwp));
            }
        }
        return list
            .GroupBy(a => a.AumId)
            .Select(g => g.First())
            .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Win32 apps by scanning Start Menu (.lnk files) per-user + common.</summary>
    public IReadOnlyList<InstalledApp> EnumerateInstalledWin32Apps()
    {
        var list = new List<InstalledApp>();
        var roots = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),  // %APPDATA%\Microsoft\Windows\Start Menu\Programs
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        };
        // Common (all-users) Start Menu — hardcoded
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        roots.Add(Path.Combine(programData, @"Microsoft\Windows\Start Menu\Programs"));
        roots.Add(Path.Combine(Environment.GetEnvironmentVariable("PROGRAMDATA") ?? @"C:\ProgramData",
                              @"Microsoft\Windows\Start Menu\Programs"));
        // Public Desktop
        roots.Add(Path.Combine(Environment.GetEnvironmentVariable("PUBLIC") ?? @"C:\Users\Public", "Desktop"));
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var lnk in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
                {
                    try
                    {
                        var (targetPath, args) = ReadShortcut(lnk);
                        if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath)) continue;
                        if (targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) continue;
                        var ext = Path.GetExtension(targetPath).ToLowerInvariant();
                        if (ext is not (".exe" or ".bat" or ".cmd")) continue;
                        var name = Path.GetFileNameWithoutExtension(lnk);
                        list.Add(new InstalledApp(
                            AumId: targetPath,
                            DisplayName: name,
                            Icon: targetPath,
                            InstallPath: targetPath,
                            Source: AppSourceKind.Win32,
                            LaunchArgs: args));
                    }
                    catch
                    {
                        // ignore unreadable .lnk
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Failed to enumerate {Root}", root);
            }
        }
        // De-dupe by target path
        return list
            .GroupBy(a => a.InstallPath ?? a.AumId, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                // Pick the shortest, nicest display name
                var best = g.OrderBy(a => a.DisplayName.Length).First();
                return first with { DisplayName = best.DisplayName };
            })
            .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Returns apps from both sources (UWP + Win32), with a Source tag.</summary>
    public IReadOnlyList<InstalledApp> EnumerateAllInstalledApps()
    {
        var all = new List<InstalledApp>();
        all.AddRange(EnumerateInstalledPwas());
        all.AddRange(EnumerateInstalledWin32Apps());
        return all
            .GroupBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public InstalledApp? FindByAumid(string aumid)
        => EnumerateInstalledPwas().FirstOrDefault(a => a.AumId == aumid);

    /// <summary>
    /// Read a .lnk (shortcut) file using the IShellLink COM interface.
    /// Returns (targetPath, args). Both may be null/empty.
    /// </summary>
    private static (string? Target, string? Args) ReadShortcut(string lnkPath)
    {
        try
        {
            // Use the IPersistFile interface to load the .lnk file
            var link = (IShellLinkW)new CShellLink();
            var persist = (IPersistFile)link;
            persist.Load(lnkPath, 0);
            var sb = new StringBuilder(260);
            link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            var target = sb.ToString();
            var argsSb = new StringBuilder(260);
            link.GetArguments(argsSb, argsSb.Capacity);
            var args = argsSb.ToString();
            return (string.IsNullOrWhiteSpace(target) ? null : target,
                    string.IsNullOrWhiteSpace(args) ? null : args);
        }
        catch
        {
            return (null, null);
        }
    }

    [System.Runtime.InteropServices.ComImport, System.Runtime.InteropServices.Guid("00021401-0000-0000-C000-000000000046")]
    private class CShellLink { }

    [System.Runtime.InteropServices.ComImport,
     System.Runtime.InteropServices.Guid("000214F9-0000-0000-C000-000000000046"),
     System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFile);
    }

    [System.Runtime.InteropServices.ComImport,
     System.Runtime.InteropServices.Guid("0000010b-0000-0000-C000-000000000046"),
     System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [System.Runtime.InteropServices.PreserveSig]
        int IsDirty();
        void Load([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string ppszFileName);
    }
}

public enum AppSourceKind
{
    Uwp = 0,
    Win32 = 1,
    Browser = 2,
    Url = 3,
    Folder = 4
}

public sealed record InstalledApp(
    string AumId,
    string DisplayName,
    string? Icon,
    string? InstallPath,
    AppSourceKind Source,
    string? LaunchArgs = null);

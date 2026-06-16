using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace WorkSpaceLauncherPro.Core.Launching;

/// <summary>
/// Launches Chromium-based browsers (Chrome / Edge / Brave / Opera GX) with a
/// specific profile directory and optional starting URL.
/// </summary>
public sealed class BrowserLauncher
{
    public sealed record BrowserInstall(string Name, string ExePath, IReadOnlyList<string> ProfileFolders);

    private readonly ILogger<BrowserLauncher> _log;

    public BrowserLauncher(ILogger<BrowserLauncher> log) => _log = log;

    public BrowserInstall? DetectChrome()
        => Detect("%LOCALAPPDATA%\\Google\\Chrome\\Application\\chrome.exe", "%LOCALAPPDATA%\\Google\\Chrome\\User Data");

    public BrowserInstall? DetectEdge()
        => Detect("C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
                  "%LOCALAPPDATA%\\Microsoft\\Edge\\User Data");

    public BrowserInstall? DetectBrave()
        => Detect("%LOCALAPPDATA%\\BraveSoftware\\Brave-Browser\\Application\\brave.exe",
                  "%LOCALAPPDATA%\\BraveSoftware\\Brave-Browser\\User Data");

    public BrowserInstall? DetectOperaGx()
        => Detect("%USERPROFILE%\\AppData\\Local\\Programs\\Opera GX\\opera.exe",
                  "%USERPROFILE%\\AppData\\Roaming\\Opera Software\\Opera GX Stable");

    public LaunchedApp? LaunchWithProfile(BrowserInstall browser, string profileFolder, string? url, bool newWindow = true)
    {
        try
        {
            var args = $"--profile-directory=\"{profileFolder}\"";
            if (newWindow) args += " --new-window";
            if (!string.IsNullOrWhiteSpace(url)) args += $" \"{url}\"";

            var psi = new ProcessStartInfo
            {
                FileName = browser.ExePath,
                Arguments = args,
                UseShellExecute = true
            };
            var p = Process.Start(psi);
            _log.LogInformation("Launched {Browser} profile={Profile} url={Url}",
                browser.Name, profileFolder, url);
            return new LaunchedApp(browser.ExePath, (uint)(p?.Id ?? 0), IntPtr.Zero);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to launch {Browser}", browser.Name);
            return null;
        }
    }

    private static BrowserInstall? Detect(string exePattern, string dataDirPattern)
    {
        var exe = Environment.ExpandEnvironmentVariables(exePattern);
        if (!File.Exists(exe)) return null;
        var dataDir = Environment.ExpandEnvironmentVariables(dataDirPattern);
        var profiles = new List<string>();
        if (Directory.Exists(dataDir))
        {
            foreach (var d in Directory.EnumerateDirectories(dataDir))
            {
                var name = Path.GetFileName(d);
                if (name == "Default" || name.StartsWith("Profile "))
                    profiles.Add(name);
            }
        }
        return new BrowserInstall(Path.GetFileNameWithoutExtension(exe), exe, profiles);
    }
}

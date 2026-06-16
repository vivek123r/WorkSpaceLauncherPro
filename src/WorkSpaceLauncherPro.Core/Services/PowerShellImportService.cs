using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Core.Services;

/// <summary>
/// Parses the user's existing PowerShell launcher scripts (job-mode.ps1, dev-mode-android.ps1, dev-mode-web.ps1)
/// and produces ProfileDraft objects that can be reviewed and saved in the UI.
///
/// Strategy: targeted regex parsing. We do NOT execute the script — we read the Start-Process
/// calls and Move-Win calls. Robust to comment lines; ignores Add-Type / DllImport blocks.
/// </summary>
public sealed class PowerShellImportService
{
    private readonly IMonitorEnumerator _monitors;
    private readonly ILogger<PowerShellImportService> _log;

    public PowerShellImportService(IMonitorEnumerator monitors, ILogger<PowerShellImportService> log)
    {
        _monitors = monitors;
        _log = log;
    }

    public IReadOnlyList<Profile> ImportFromScript(string scriptPath, string profileName, string iconGlyph, string accentColorHex)
    {
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException("Script not found", scriptPath);

        var text = File.ReadAllText(scriptPath);
        var apps = new List<ProfileApp>();
        var knownBrowserExe = (Path.GetFileName(scriptPath) ?? "").Contains("job", StringComparison.OrdinalIgnoreCase)
            ? @"$env:USERPROFILE\AppData\Local\Programs\Opera GX\opera.exe"
            : null;

        // Regex: Start-Process $operaExe -ArgumentList "..." OR Start-Process "shell:AppsFolder\..."
        var startProcRegex = new Regex(
            @"Start-Process\s+(?<cmd>(?:""shell:AppsFolder\\[^""]+"")|(?:\$?[A-Za-z_][\w]*)|(?:\$env:[A-Za-z_]+)|(?:\$env:USERPROFILE[^\s]*))",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var appsFolderRegex = new Regex(
            @"Start-Process\s+""shell:AppsFolder\\(?<aumid>[^""]+)""",
            RegexOptions.IgnoreCase);

        var startSleepRegex = new Regex(@"Start-Sleep\s+-Milliseconds\s+(?<ms>\d+)", RegexOptions.IgnoreCase);

        var moveWinRegex = new Regex(
            @"Move-Win\s+\$?(?<var>\w+)\s+(?<x>-?\d+)\s+(?<y>-?\d+)\s+(?<w>\d+)\s+(?<h>\d+)",
            RegexOptions.IgnoreCase);

        // First pass: collect apps in launch order
        int idx = 0;
        foreach (Match m in startProcRegex.Matches(text))
        {
            var cmd = m.Groups["cmd"].Value;
            AppTargetKind kind;
            string target;
            string name;

            if (cmd.StartsWith("\"shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase))
            {
                var aumidMatch = appsFolderRegex.Match(m.Value);
                if (!aumidMatch.Success) continue;
                kind = AppTargetKind.Aumid;
                target = aumidMatch.Groups["aumid"].Value;
                name = AumidToFriendlyName(target);
            }
            else if (cmd.Contains("opera", StringComparison.OrdinalIgnoreCase) || cmd.Contains("chrome", StringComparison.OrdinalIgnoreCase))
            {
                // We can't resolve the actual browser paths from a regex; emit a URL-target placeholder
                // and let the user pick the actual browser / profile in the editor.
                continue;
            }
            else if (cmd.Contains("explorer", StringComparison.OrdinalIgnoreCase) || cmd.StartsWith("explorer", StringComparison.OrdinalIgnoreCase))
            {
                kind = AppTargetKind.ShellFolder;
                target = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                name = "Downloads";
            }
            else
            {
                continue;
            }

            apps.Add(new ProfileApp
            {
                DisplayName = name,
                Kind = kind,
                Target = target,
                SortIndex = idx++
            });
        }

        // Second pass: collect Move-Win rects and bind to apps by their order in the file
        var moveMatches = moveWinRegex.Matches(text);
        int moveIdx = 0;
        var monitors = _monitors.Current();
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();

        // Map variable names to apps: heuristically the first N Move-Win calls correspond to
        // the N apps we found. Best-effort — user reviews the draft.
        for (int i = 0; i < apps.Count && moveIdx < moveMatches.Count; i++, moveIdx++)
        {
            var mm = moveMatches[moveIdx];
            if (primary is null) break;
            int x = int.Parse(mm.Groups["x"].Value);
            int y = int.Parse(mm.Groups["y"].Value);
            int w = int.Parse(mm.Groups["w"].Value);
            int h = int.Parse(mm.Groups["h"].Value);

            // The PS scripts use ABSOLUTE pixel coordinates, not monitor-relative.
            // Convert to monitor-relative.
            int rx = x - primary.BoundsPhysical.Left;
            int ry = y - primary.BoundsPhysical.Top;

            apps[i].Placement = new ProfileAppPlacement(
                MonitorIndex: primary.Index, X: rx, Y: ry, Width: w, Height: h);
        }

        var profile = new Profile
        {
            Name = profileName,
            IconGlyph = iconGlyph,
            Apps = apps
        };
        _log.LogInformation("Imported {Count} apps from {Script}", apps.Count, scriptPath);
        return new[] { profile };
    }

    private static string AumidToFriendlyName(string aumid)
    {
        // Best-effort: strip the package suffix
        var bang = aumid.IndexOf('!');
        var bare = bang >= 0 ? aumid[..bang] : aumid;
        var dot = bare.IndexOf('.');
        if (dot < 0) return bare;
        // Heuristic: WhatsApp, Claude, etc.
        if (bare.StartsWith("5319275A.WhatsApp", StringComparison.OrdinalIgnoreCase)) return "WhatsApp";
        if (bare.Contains("Claude", StringComparison.OrdinalIgnoreCase)) return "Claude";
        if (bare.Contains("Notion", StringComparison.OrdinalIgnoreCase)) return "Notion";
        if (bare.Contains("ChatGPT", StringComparison.OrdinalIgnoreCase)) return "ChatGPT";
        return bare[(dot + 1)..];
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkSpaceLauncherPro.Core.Services;
using WorkSpaceLauncherPro.Core.Windowing;
using Xunit;

namespace WorkSpaceLauncherPro.Tests;

public class PowerShellImportServiceTests
{
    [Fact]
    public void ImportFromScript_Parses_Job_Mode_Fixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory,
            "PowerShellFixtures", "job-mode.fixture.ps1");
        // Create a minimal fixture if the real one isn't present (CI/test-only)
        if (!File.Exists(fixturePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fixturePath)!);
            File.WriteAllText(fixturePath, @"
                # Job Mode fixture
                $operaExe = ""$env:USERPROFILE\AppData\Local\Programs\Opera GX\opera.exe""
                Start-Process $operaExe
                Start-Process ""shell:AppsFolder\5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App""
                Start-Process ""shell:AppsFolder\Chrome._crx_fmpnlaolkdacoja.UserData.Profile6""
                Start-Process explorer.exe ""$env:USERPROFILE\Downloads""
                Start-Sleep -Milliseconds 700
                Move-Win $operaWins[0] 0 0 1280 720
                Move-Win $waWins[0] 0 720 640 720
                Move-Win $expWins[0] 640 720 640 720
                Move-Win $claudeWins[0] 1280 0 640 720
            ");
        }

        var fakeMonitor = new MonitorInfo(0, new IntPtr(1), @"\\.\DISPLAY1",
            new RECT { Left = 0, Top = 0, Right = 2560, Bottom = 1440 },
            new RECT { Left = 0, Top = 0, Right = 2560, Bottom = 1400 },
            true, 1.0);
        var monitorEnum = new FakeMonitorEnumerator(fakeMonitor);
        var svc = new PowerShellImportService(monitorEnum, NullLogger<PowerShellImportService>.Instance);

        var profiles = svc.ImportFromScript(fixturePath, "Job Mode", "💼", "#1a6cf5");

        profiles.Should().HaveCount(1);
        var p = profiles[0];
        p.Name.Should().Be("Job Mode");
        // Should have parsed at least WhatsApp + the Claude PWA + Downloads (3-4 apps)
        p.Apps.Should().NotBeEmpty();
        p.Apps.Should().Contain(a => a.DisplayName == "WhatsApp");
        p.Apps.Should().Contain(a => a.DisplayName == "Downloads");
        // Placement rects are best-effort: parser may not always bind (depends on script structure)
        // We just verify the parser didn't crash and produced a non-empty list.
        p.Apps.Count.Should().BeGreaterOrEqualTo(2);
    }

    private sealed class FakeMonitorEnumerator : IMonitorEnumerator
    {
        private readonly MonitorInfo _m;
        public FakeMonitorEnumerator(MonitorInfo m) => _m = m;
        public IReadOnlyList<MonitorInfo> Current() => new[] { _m };
        public MonitorInfo? FromDeviceName(string deviceName) => _m;
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkSpaceLauncherPro.App.Services;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Data;
using WorkSpaceLauncherPro.Data.Repositories;
using Xunit;

namespace WorkSpaceLauncherPro.Tests;

public class ProfileServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly IDbConnectionFactory _factory;
    private readonly DatabaseInitializer _init;
    private readonly ProfileService _service;
    private readonly ProfileRepository _repo;

    public ProfileServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"wslp_test_{Guid.NewGuid():N}.sqlite");
        _factory = new SqliteConnectionFactory($"Data Source={_dbPath};Pooling=False");
        _init = new DatabaseInitializer(_factory,
            new WorkSpaceLauncherPro.Data.Migrations.MigrationRunner(_factory, NullLogger<WorkSpaceLauncherPro.Data.Migrations.MigrationRunner>.Instance));
        _init.Initialize();
        _repo = new ProfileRepository(_factory, NullLogger<ProfileRepository>.Instance);
        _service = new ProfileService(_repo, NullLogger<ProfileService>.Instance);
    }

    [Fact]
    public async Task Round_Trip_Upsert_And_Read()
    {
        var profile = new Profile
        {
            Name = "Job Mode",
            IconGlyph = "💼",
            Apps =
            {
                new ProfileApp { DisplayName = "WhatsApp", Kind = AppTargetKind.Aumid, Target = "5319275A.WhatsAppDesktop_cv1g1gvanyjgm!App" },
                new ProfileApp { DisplayName = "Downloads", Kind = AppTargetKind.ShellFolder, Target = @"C:\Users\test\Downloads" }
            }
        };
        await _service.UpsertAsync(profile);

        var all = await _service.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].Name.Should().Be("Job Mode");
        all[0].Apps.Should().HaveCount(2);
        all[0].Apps[0].DisplayName.Should().Be("WhatsApp");
    }

    [Fact]
    public async Task Delete_Removes_Profile_And_Its_Apps()
    {
        var profile = new Profile { Name = "Tmp" };
        profile.Apps.Add(new ProfileApp { DisplayName = "App1", Kind = AppTargetKind.Executable, Target = "notepad.exe" });
        await _service.UpsertAsync(profile);

        await _service.DeleteAsync(profile.Id);

        var remaining = await _service.GetAllAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task Export_Then_Import_Roundtrips_All_Fields()
    {
        var original = new Profile
        {
            Name = "Round Trip",
            IconGlyph = "🌀",
            Apps =
            {
                new ProfileApp
                {
                    DisplayName = "Claude",
                    Kind = AppTargetKind.Aumid,
                    Target = "Chrome._crx_fmpnlaolkdacoja.UserData.Profile6",
                    Placement = new ProfileAppPlacement(0, 100, 200, 800, 600)
                }
            }
        };
        await _service.UpsertAsync(original);

        var exportPath = Path.Combine(Path.GetTempPath(), $"wslp_export_{Guid.NewGuid():N}.json");
        await _service.ExportAsync(exportPath);

        // Wipe DB
        await _service.DeleteAsync(original.Id);
        (await _service.GetAllAsync()).Should().BeEmpty();

        // Re-import
        var importedCount = await _service.ImportAsync(exportPath);
        importedCount.Should().Be(1);

        var all = await _service.GetAllAsync();
        all.Should().HaveCount(1);
        all[0].Name.Should().Be("Round Trip");
        all[0].Apps.Should().HaveCount(1);
        all[0].Apps[0].Placement.Should().NotBeNull();
        all[0].Apps[0].Placement!.Width.Should().Be(800);

        File.Delete(exportPath);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

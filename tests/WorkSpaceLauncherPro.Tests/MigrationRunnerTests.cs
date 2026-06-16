using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WorkSpaceLauncherPro.Data;
using WorkSpaceLauncherPro.Data.Migrations;
using WorkSpaceLauncherPro.Data.Repositories;
using Xunit;

namespace WorkSpaceLauncherPro.Tests;

public class MigrationRunnerTests : IDisposable
{
    private readonly string _dbPath;
    private readonly IDbConnectionFactory _factory;
    private readonly DatabaseInitializer _init;

    public MigrationRunnerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"wslp_mig_{Guid.NewGuid():N}.sqlite");
        _factory = new SqliteConnectionFactory($"Data Source={_dbPath};Pooling=False");
        _init = new DatabaseInitializer(_factory,
            new MigrationRunner(_factory, NullLogger<MigrationRunner>.Instance));
    }

    [Fact]
    public void First_Run_Creates_All_Tables()
    {
        _init.Initialize();
        using var conn = _factory.Create();
        conn.Open();
        var tables = new[] { "schema_version", "profiles", "profile_apps", "monitor_snapshots", "app_settings" };
        foreach (var t in tables)
        {
            var exists = conn.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name",
                new { name = t });
            exists.Should().Be(1, $"table {t} should exist after init");
        }
    }

    [Fact]
    public void Second_Run_Is_Idempotent()
    {
        _init.Initialize();
        var act = () => _init.Initialize(); // should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void Default_Settings_Are_Seeded()
    {
        _init.Initialize();
        using var conn = _factory.Create();
        conn.Open();
        var startup = conn.ExecuteScalar<string>(
            "SELECT value FROM app_settings WHERE key='startup_enabled'");
        startup.Should().Be("false");
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

internal static class DapperExtensions
{
    public static T? ExecuteScalar<T>(this IDbConnection conn, string sql, object? param = null)
        => Dapper.SqlMapper.ExecuteScalar<T>(conn, sql, param);
}

using Dapper;
using Microsoft.Extensions.Logging;

namespace WorkSpaceLauncherPro.Data.Migrations;

public sealed class MigrationRunner
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<MigrationRunner> _log;

    // Ordered list of available migrations. Add new entries to the END.
    public IReadOnlyList<IMigration> Migrations { get; }

    public MigrationRunner(IDbConnectionFactory factory, ILogger<MigrationRunner> log)
    {
        _factory = factory;
        _log = log;
        Migrations = new IMigration[] { new InitialMigration() };
    }

    public void Run()
    {
        using var conn = _factory.Create();
        conn.Open();

        // Ensure schema_version table exists (idempotent — also done by migration 1)
        conn.Execute(@"CREATE TABLE IF NOT EXISTS schema_version (
                          version INTEGER PRIMARY KEY,
                          applied_at TEXT NOT NULL);");

        var currentVersion = conn.QuerySingleOrDefault<int?>("SELECT MAX(version) FROM schema_version") ?? 0;
        _log.LogInformation("Current schema version: {Version}", currentVersion);

        using var tx = conn.BeginTransaction();
        foreach (var mig in Migrations)
        {
            if (mig.Version <= currentVersion) continue;
            _log.LogInformation("Applying migration v{Version} ({Name})", mig.Version, mig.Name);
            foreach (var sql in mig.Statements)
            {
                conn.Execute(sql, transaction: tx);
            }
            conn.Execute(
                "INSERT INTO schema_version(version, applied_at) VALUES(@v, @t)",
                new { v = mig.Version, t = DateTime.UtcNow.ToString("o") },
                transaction: tx);
        }
        tx.Commit();
        _log.LogInformation("Migrations complete. Now at v{Version}", Migrations.Max(m => m.Version));
    }
}

public interface IMigration
{
    int Version { get; }
    string Name { get; }
    IReadOnlyList<string> Statements { get; }
}

public sealed class InitialMigration : IMigration
{
    public int Version => Migration_001_Initial.Version;
    public string Name => "Initial schema (profiles, apps, snapshots, settings)";
    public IReadOnlyList<string> Statements => Migration_001_Initial.Statements;
}

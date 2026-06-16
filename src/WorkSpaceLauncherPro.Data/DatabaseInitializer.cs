using WorkSpaceLauncherPro.Data.Migrations;

namespace WorkSpaceLauncherPro.Data;

/// <summary>
/// Top-level database initializer: ensures the SQLite file path exists and
/// applies all pending migrations. Safe to call on every startup.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;
    private readonly MigrationRunner _runner;

    public DatabaseInitializer(IDbConnectionFactory factory, MigrationRunner runner)
    {
        _factory = factory;
        _runner = runner;
    }

    public void Initialize()
    {
        // Ensure connection string points to a valid directory.
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(_factory.ConnectionString);
        if (!string.IsNullOrEmpty(builder.DataSource) && builder.DataSource != ":memory:")
        {
            var dir = System.IO.Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(dir))
                System.IO.Directory.CreateDirectory(dir);
        }

        _runner.Run();
    }
}

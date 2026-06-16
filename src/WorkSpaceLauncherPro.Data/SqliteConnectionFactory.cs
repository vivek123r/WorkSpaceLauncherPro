using System.Data;
using Microsoft.Data.Sqlite;

namespace WorkSpaceLauncherPro.Data;

public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public SqliteConnectionFactory(string connectionString) => ConnectionString = connectionString;

    public IDbConnection Create() => new SqliteConnection(ConnectionString);
}

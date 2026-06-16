using Dapper;

namespace WorkSpaceLauncherPro.Data.Repositories;

public sealed class MonitorSnapshotRepository : IMonitorSnapshotRepository
{
    private readonly IDbConnectionFactory _factory;
    public MonitorSnapshotRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Guid> SaveAsync(string layoutJson, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        var id = Guid.NewGuid();
        await conn.ExecuteAsync(
            "INSERT INTO monitor_snapshots(id, captured_at, layout_json) VALUES(@id, @t, @j)",
            new { id = id.ToString(), t = DateTime.UtcNow.ToString("o"), j = layoutJson });
        return id;
    }

    public async Task<string?> GetLatestAsync(CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        return await conn.QuerySingleOrDefaultAsync<string?>(
            "SELECT layout_json FROM monitor_snapshots ORDER BY captured_at DESC LIMIT 1");
    }
}

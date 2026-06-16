using Dapper;

namespace WorkSpaceLauncherPro.Data.Repositories;

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly IDbConnectionFactory _factory;

    public SettingsRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        return await conn.QuerySingleOrDefaultAsync<string?>(
            "SELECT value FROM app_settings WHERE key = @key", new { key });
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        await conn.ExecuteAsync(@"
            INSERT INTO app_settings(key, value) VALUES(@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value",
            new { key, value });
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        var rows = await conn.QueryAsync<(string Key, string Value)>(
            "SELECT key AS Key, value AS Value FROM app_settings");
        return rows.ToDictionary(r => r.Key, r => r.Value);
    }
}

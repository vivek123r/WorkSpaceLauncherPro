namespace WorkSpaceLauncherPro.Data.Repositories;

public interface IMonitorSnapshotRepository
{
    Task<Guid> SaveAsync(string layoutJson, CancellationToken ct = default);
    Task<string?> GetLatestAsync(CancellationToken ct = default);
}

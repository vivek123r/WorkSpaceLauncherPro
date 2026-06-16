using WorkSpaceLauncherPro.Core.Models;

namespace WorkSpaceLauncherPro.Data.Repositories;

public interface IProfileRepository
{
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken ct = default);
    Task<Profile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpsertAsync(Profile profile, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> ReorderAsync(IEnumerable<Guid> orderedIds, CancellationToken ct = default);
}

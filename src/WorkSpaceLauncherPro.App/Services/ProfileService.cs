using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Data.Repositories;

namespace WorkSpaceLauncherPro.App.Services;

public sealed class ProfileService
{
    private readonly IProfileRepository _repo;
    private readonly ILogger<ProfileService> _log;

    public ProfileService(IProfileRepository repo, ILogger<ProfileService> log)
    {
        _repo = repo;
        _log = log;
    }

    public Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);

    public Task<Profile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task UpsertAsync(Profile profile, CancellationToken ct = default)
        => _repo.UpsertAsync(profile, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);

    public async Task ExportAsync(string path, CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        var export = new WorkspaceExport
        {
            Version = 1,
            ExportedAt = DateTime.UtcNow,
            Profiles = all.ToList()
        };
        await File.WriteAllTextAsync(path,
            JsonSerializer.Serialize(export, ProfileJsonContext.Default.WorkspaceExport), ct);
        _log.LogInformation("Exported {Count} profiles to {Path}", all.Count, path);
    }

    public async Task<int> ImportAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        var export = JsonSerializer.Deserialize(json, ProfileJsonContext.Default.WorkspaceExport)
            ?? throw new InvalidDataException("Empty or invalid profile file.");
        if (export.Version != 1)
            throw new InvalidDataException($"Unsupported export version: {export.Version}");

        int imported = 0;
        foreach (var p in export.Profiles)
        {
            p.Id = Guid.NewGuid();
            p.UpdatedAt = DateTime.UtcNow;
            foreach (var a in p.Apps) a.Id = Guid.NewGuid();
            await _repo.UpsertAsync(p, ct);
            imported++;
        }
        _log.LogInformation("Imported {Count} profiles from {Path}", imported, path);
        return imported;
    }
}

public sealed class WorkspaceExport
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public List<Profile> Profiles { get; set; } = new();
}

[JsonSerializable(typeof(WorkspaceExport))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(System.Windows.Media.Color), TypeInfoPropertyName = "Color", GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ProfileJsonContext : JsonSerializerContext
{
}

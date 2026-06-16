using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Models;

namespace WorkSpaceLauncherPro.Data.Repositories;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<ProfileRepository> _log;

    static ProfileRepository()
    {
        // Map snake_case columns (created_at) to PascalCase props (CreatedAt)
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public ProfileRepository(IDbConnectionFactory factory, ILogger<ProfileRepository> log)
    {
        _factory = factory;
        _log = log;
    }

    public async Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();

        var rows = (await conn.QueryAsync<ProfileRow>(
            "SELECT * FROM profiles ORDER BY sort_index, name")).ToList();
        var appRows = (await conn.QueryAsync<ProfileAppRow>(
            "SELECT * FROM profile_apps ORDER BY sort_index")).ToList();

        var result = new List<Profile>(rows.Count);
        foreach (var r in rows)
        {
            var apps = appRows
                .Where(a => a.ProfileId == r.Id)
                .Select(ProfileAppRow.ToModel)
                .ToList();
            result.Add(ProfileRow.ToModel(r, apps));
        }
        return result;
    }

    public async Task<Profile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        var row = await conn.QuerySingleOrDefaultAsync<ProfileRow>(
            "SELECT * FROM profiles WHERE id = @id", new { id = id.ToString() });
        if (row is null) return null;

        var apps = (await conn.QueryAsync<ProfileAppRow>(
            "SELECT * FROM profile_apps WHERE profile_id = @id ORDER BY sort_index",
            new { id = id.ToString() })).ToList();

        return ProfileRow.ToModel(row, apps.Select(ProfileAppRow.ToModel).ToList());
    }

    public async Task UpsertAsync(Profile profile, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();

        var p = ProfileRow.FromModel(profile);
        await conn.ExecuteAsync(@"
            INSERT INTO profiles(id, name, icon_glyph, accent_color, hotkey_mod, hotkey_vk,
                                 default_layout, target_monitor_device, sort_index, created_at, updated_at)
            VALUES(@Id, @Name, @IconGlyph, @AccentColor, @HotkeyMod, @HotkeyVk,
                   @DefaultLayout, @TargetMonitorDevice, @SortIndex, @CreatedAt, @UpdatedAt)
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                icon_glyph = excluded.icon_glyph,
                accent_color = excluded.accent_color,
                hotkey_mod = excluded.hotkey_mod,
                hotkey_vk = excluded.hotkey_vk,
                default_layout = excluded.default_layout,
                target_monitor_device = excluded.target_monitor_device,
                sort_index = excluded.sort_index,
                updated_at = excluded.updated_at",
            p, transaction: tx);

        // Replace apps
        await conn.ExecuteAsync(
            "DELETE FROM profile_apps WHERE profile_id = @id",
            new { id = p.Id }, transaction: tx);

        var index = 0;
        foreach (var app in profile.Apps)
        {
            var a = ProfileAppRow.FromModel(profile.Id, app, index++);
            await conn.ExecuteAsync(@"
                INSERT INTO profile_apps(id, profile_id, display_name, kind, target,
                                         browser_profile_dir, launch_args, match_title, match_process,
                                         placement_x, placement_y, placement_w, placement_h,
                                         placement_monitor, placement_state, launch_delay_ms, sort_index)
                VALUES(@Id, @ProfileId, @DisplayName, @Kind, @Target,
                       @BrowserProfileDir, @LaunchArgs, @MatchTitle, @MatchProcess,
                       @PlacementX, @PlacementY, @PlacementW, @PlacementH,
                       @PlacementMonitor, @PlacementState, @LaunchDelayMs, @SortIndex)",
                a, transaction: tx);
        }

        tx.Commit();
        _log.LogInformation("Upserted profile {Id} ({Name}) with {Count} apps",
            profile.Id, profile.Name, profile.Apps.Count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        await conn.ExecuteAsync("DELETE FROM profiles WHERE id = @id", new { id = id.ToString() });
    }

    public async Task<int> ReorderAsync(IEnumerable<Guid> orderedIds, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();
        var i = 0;
        foreach (var id in orderedIds)
        {
            await conn.ExecuteAsync(
                "UPDATE profiles SET sort_index = @i WHERE id = @id",
                new { i = i++, id = id.ToString() },
                transaction: tx);
        }
        tx.Commit();
        return i;
    }
}

// ===== Storage row types (decoupled from domain models) =====

internal sealed class ProfileRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string IconGlyph { get; set; } = "";
    public string AccentColor { get; set; } = "#1a6cf5";
    public int HotkeyMod { get; set; }
    public int HotkeyVk { get; set; }
    public string? DefaultLayout { get; set; }
    public string? TargetMonitorDevice { get; set; }
    public int SortIndex { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";

    public static Profile ToModel(ProfileRow r, IReadOnlyList<ProfileApp> apps) => new()
    {
        Id = Guid.Parse(r.Id),
        Name = r.Name,
        IconGlyph = r.IconGlyph,
        AccentColor = ParseColor(r.AccentColor),
        Hotkey = new Hotkey(r.HotkeyMod, r.HotkeyVk),
        DefaultLayout = r.DefaultLayout,
        TargetMonitorDevice = r.TargetMonitorDevice,
        SortIndex = r.SortIndex,
        CreatedAt = DateTime.Parse(r.CreatedAt),
        UpdatedAt = DateTime.Parse(r.UpdatedAt),
        Apps = apps.ToList()
    };

    public static ProfileRow FromModel(Profile p) => new()
    {
        Id = p.Id.ToString(),
        Name = p.Name,
        IconGlyph = p.IconGlyph,
        AccentColor = p.AccentColor.ToString().ToUpperInvariant(),
        HotkeyMod = p.Hotkey.Modifiers,
        HotkeyVk = p.Hotkey.VirtualKey,
        DefaultLayout = p.DefaultLayout,
        TargetMonitorDevice = p.TargetMonitorDevice,
        SortIndex = p.SortIndex,
        CreatedAt = p.CreatedAt.ToString("o"),
        UpdatedAt = p.UpdatedAt.ToString("o")
    };

    private static System.Windows.Media.Color ParseColor(string hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];
        byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
        return System.Windows.Media.Color.FromRgb(r, g, b);
    }
}

internal sealed class ProfileAppRow
{
    public string Id { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Kind { get; set; }
    public string Target { get; set; } = "";
    public string? BrowserProfileDir { get; set; }
    public string? LaunchArgs { get; set; }
    public string? MatchTitle { get; set; }
    public string? MatchProcess { get; set; }
    public int? PlacementX { get; set; }
    public int? PlacementY { get; set; }
    public int? PlacementW { get; set; }
    public int? PlacementH { get; set; }
    public int PlacementMonitor { get; set; }
    public int PlacementState { get; set; } = 1;
    public int LaunchDelayMs { get; set; }
    public int SortIndex { get; set; }

    public static ProfileApp ToModel(ProfileAppRow r)
    {
        ProfileAppPlacement? placement = null;
        if (r.PlacementX is not null && r.PlacementY is not null
            && r.PlacementW is not null && r.PlacementH is not null)
        {
            placement = new ProfileAppPlacement(
                r.PlacementMonitor, r.PlacementX.Value, r.PlacementY.Value,
                r.PlacementW.Value, r.PlacementH.Value,
                (ShowState)r.PlacementState);
        }
        return new ProfileApp
        {
            Id = Guid.Parse(r.Id),
            DisplayName = r.DisplayName,
            Kind = (AppTargetKind)r.Kind,
            Target = r.Target,
            BrowserProfileDir = r.BrowserProfileDir,
            LaunchArgs = r.LaunchArgs,
            MatchRule = string.IsNullOrEmpty(r.MatchTitle) && string.IsNullOrEmpty(r.MatchProcess)
                ? null
                : new WindowMatchRule(r.MatchTitle, r.MatchProcess),
            Placement = placement,
            LaunchDelayMs = r.LaunchDelayMs,
            SortIndex = r.SortIndex
        };
    }

    public static ProfileAppRow FromModel(Guid profileId, ProfileApp a, int sortIndex) => new()
    {
        Id = a.Id.ToString(),
        ProfileId = profileId.ToString(),
        DisplayName = a.DisplayName,
        Kind = (int)a.Kind,
        Target = a.Target,
        BrowserProfileDir = a.BrowserProfileDir,
        LaunchArgs = a.LaunchArgs,
        MatchTitle = a.MatchRule?.TitleContains,
        MatchProcess = a.MatchRule?.ProcessName,
        PlacementX = a.Placement?.X,
        PlacementY = a.Placement?.Y,
        PlacementW = a.Placement?.Width,
        PlacementH = a.Placement?.Height,
        PlacementMonitor = a.Placement?.MonitorIndex ?? 0,
        PlacementState = (int)(a.Placement?.ShowState ?? ShowState.Normal),
        LaunchDelayMs = a.LaunchDelayMs,
        SortIndex = sortIndex
    };
}

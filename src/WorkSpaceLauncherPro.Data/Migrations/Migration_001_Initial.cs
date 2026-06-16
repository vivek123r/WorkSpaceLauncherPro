namespace WorkSpaceLauncherPro.Data.Migrations;

/// <summary>
/// v1 schema. See plan §7 for full DDL. Hand-rolled — no EF Core.
/// </summary>
public static class Migration_001_Initial
{
    public const int Version = 1;

    public static readonly string[] Statements =
    {
        // schema version
        @"CREATE TABLE IF NOT EXISTS schema_version (
              version     INTEGER PRIMARY KEY,
              applied_at  TEXT    NOT NULL
          );",

        // profiles
        @"CREATE TABLE IF NOT EXISTS profiles (
              id           TEXT PRIMARY KEY,
              name         TEXT NOT NULL,
              icon_glyph   TEXT NOT NULL DEFAULT '🚀',
              accent_color TEXT NOT NULL DEFAULT '#1a6cf5',
              hotkey_mod   INTEGER NOT NULL DEFAULT 0,
              hotkey_vk    INTEGER NOT NULL DEFAULT 0,
              default_layout TEXT,
              target_monitor_device TEXT,
              sort_index   INTEGER NOT NULL DEFAULT 0,
              created_at   TEXT NOT NULL,
              updated_at   TEXT NOT NULL
          );",

        // profile_apps
        @"CREATE TABLE IF NOT EXISTS profile_apps (
              id                  TEXT PRIMARY KEY,
              profile_id          TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
              display_name        TEXT NOT NULL,
              kind                INTEGER NOT NULL,
              target              TEXT NOT NULL,
              browser_profile_dir TEXT,
              launch_args         TEXT,
              match_title         TEXT,
              match_process       TEXT,
              placement_x         INTEGER,
              placement_y         INTEGER,
              placement_w         INTEGER,
              placement_h         INTEGER,
              placement_monitor   INTEGER NOT NULL DEFAULT 0,
              placement_state     INTEGER NOT NULL DEFAULT 1,
              launch_delay_ms     INTEGER NOT NULL DEFAULT 0,
              sort_index          INTEGER NOT NULL DEFAULT 0
          );",
        @"CREATE INDEX IF NOT EXISTS ix_profile_apps_profile
              ON profile_apps(profile_id, sort_index);",

        // monitor_snapshots
        @"CREATE TABLE IF NOT EXISTS monitor_snapshots (
              id          TEXT PRIMARY KEY,
              captured_at TEXT NOT NULL,
              layout_json TEXT NOT NULL
          );",

        // app_settings
        @"CREATE TABLE IF NOT EXISTS app_settings (
              key   TEXT PRIMARY KEY,
              value TEXT NOT NULL
          );",

        // seed default settings
        @"INSERT OR IGNORE INTO app_settings(key, value) VALUES
              ('startup_enabled',         'false'),
              ('hotkey_quickpicker',      'Ctrl+Alt+Space'),
              ('hotkey_quickpicker_enabled', 'true'),
              ('theme',                   'dark');"
    };
}

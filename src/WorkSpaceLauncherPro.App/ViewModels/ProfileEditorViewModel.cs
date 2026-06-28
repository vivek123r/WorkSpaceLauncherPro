using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Data.Repositories;
using WorkSpaceLauncherPro.App.Views;
using MediaColor = System.Windows.Media.Color;

namespace WorkSpaceLauncherPro.App.ViewModels;

public sealed partial class AppRowViewModel : ObservableObject
{
    public ProfileApp Source { get; }

    public AppRowViewModel(ProfileApp source)
    {
        Source = source;
    }

    public Guid Id => Source.Id;
    public AppTargetKind Kind => Source.Kind;

    public string KindLabel => Kind switch
    {
        AppTargetKind.Executable => "App / EXE",
        AppTargetKind.Aumid => "UWP / PWA",
        AppTargetKind.Url => "URL",
        AppTargetKind.BrowserProfile => "Browser Profile",
        AppTargetKind.ShellFolder => "Folder",
        _ => Kind.ToString()
    };

    public string DisplayName
    {
        get => Source.DisplayName;
        set { if (Source.DisplayName != value) { Source.DisplayName = value; OnPropertyChanged(); } }
    }

    public string Target
    {
        get => Source.Target;
        set { if (Source.Target != value) { Source.Target = value; OnPropertyChanged(); } }
    }

    public string? LaunchArgs
    {
        get => Source.LaunchArgs;
        set { if (Source.LaunchArgs != value) { Source.LaunchArgs = value; OnPropertyChanged(); } }
    }

    public string? BrowserProfileDir
    {
        get => Source.BrowserProfileDir;
        set { if (Source.BrowserProfileDir != value) { Source.BrowserProfileDir = value; OnPropertyChanged(); } }
    }

    public int LaunchDelayMs
    {
        get => Source.LaunchDelayMs;
        set { if (Source.LaunchDelayMs != value) { Source.LaunchDelayMs = value; OnPropertyChanged(); } }
    }

    public string Summary
    {
        get
        {
            var prefix = Kind switch
            {
                AppTargetKind.BrowserProfile => $"[{Source.BrowserProfileDir}] ",
                _ => ""
            };
            var tail = string.IsNullOrWhiteSpace(Source.LaunchArgs) ? "" : $"  {Source.LaunchArgs}";
            return $"{prefix}{Source.Target}{tail}";
        }
    }
}

public sealed partial class ProfileEditorViewModel : ObservableObject
{
    private readonly IProfileRepository _repo;
    private readonly ILogger<ProfileEditorViewModel> _log;
    private readonly Profile _profile;
    private readonly bool _isNew;

    public Guid Id => _profile.Id;
    public bool IsNew => _isNew;

    [ObservableProperty]
    private string _name = "New Profile";

    [ObservableProperty]
    private string _iconGlyph = "🚀";

    [ObservableProperty]
    private MediaColor _accentColor = MediaColor.FromRgb(0x1A, 0x6C, 0xF5);

    [ObservableProperty]
    private string? _defaultLayout;

    [ObservableProperty]
    private string? _hotkeyDisplay;

    [ObservableProperty]
    private string _statusText = "";

    public ObservableCollection<AppRowViewModel> Apps { get; } = new();

    // Preset accent colors
    public MediaColor[] AccentPresets { get; } = new[]
    {
        MediaColor.FromRgb(0x1A, 0x6C, 0xF5), // blue
        MediaColor.FromRgb(0x6C, 0x5C, 0xE7), // purple
        MediaColor.FromRgb(0x00, 0xB8, 0x94), // teal
        MediaColor.FromRgb(0xF5, 0x9E, 0x42), // orange
        MediaColor.FromRgb(0xEF, 0x44, 0x44), // red
        MediaColor.FromRgb(0x10, 0xB9, 0x81), // green
        MediaColor.FromRgb(0xEC, 0x48, 0x99), // pink
        MediaColor.FromRgb(0x64, 0x74, 0x8B), // slate
    };

    // Preset icon glyphs
    public string[] IconPresets { get; } = new[]
    {
        "🚀", "💼", "🎮", "🎨", "📺", "💻", "📱", "🛠️",
        "📚", "🎵", "🏠", "✈️", "🧪", "📊", "🛒", "⚙️"
    };

    // Browser options
    public string[] BrowserKinds { get; } = new[] { "chrome", "edge", "brave", "opera" };
    public string[] BrowserProfileDirs { get; } = new[] { "Default", "Profile 1", "Profile 2", "Profile 3", "Profile 4", "Profile 5" };

    [ObservableProperty]
    private string _newAppKind = "Executable";

    [ObservableProperty]
    private string _newAppName = "";

    [ObservableProperty]
    private string _newAppTarget = "";

    [ObservableProperty]
    private string? _newAppArgs;

    [ObservableProperty]
    private string _newAppBrowser = "chrome";

    [ObservableProperty]
    private string _newAppProfileDir = "Default";

    public string[] NewAppKinds { get; } = new[] { "Executable", "UWP / PWA", "URL", "Browser Profile", "Folder" };

    public ProfileEditorViewModel() : this(null, null) { }

    public ProfileEditorViewModel(Profile? existing, IProfileRepository? repo = null, ILogger<ProfileEditorViewModel>? log = null)
    {
        _isNew = existing is null;
        _profile = existing ?? new Profile();
        _repo = repo ?? App.Services.GetRequiredService<IProfileRepository>();
        _log = log ?? App.Services.GetRequiredService<ILogger<ProfileEditorViewModel>>();

        Name = _profile.Name;
        IconGlyph = _profile.IconGlyph;
        AccentColor = _profile.AccentColor;
        DefaultLayout = _profile.DefaultLayout;
        HotkeyDisplay = _profile.Hotkey.IsSet
            ? $"Ctrl+Alt+{(char)_profile.Hotkey.VirtualKey}"
            : "(none)";

        foreach (var a in _profile.Apps.OrderBy(x => x.SortIndex))
            Apps.Add(new AppRowViewModel(a));
    }

    [RelayCommand]
    private void PickAccent(MediaColor c) => AccentColor = c;

    [RelayCommand]
    private void PickIcon(string g) => IconGlyph = g;

    [RelayCommand]
    private void AddApp()
    {
        if (string.IsNullOrWhiteSpace(NewAppTarget)) { StatusText = "Target is required."; return; }
        if (string.IsNullOrWhiteSpace(NewAppName))
            NewAppName = NewAppTarget.Split('\\', '/').Last();

        var (kind, target, args, profileDir) = NewAppKind switch
        {
            "Executable" => (AppTargetKind.Executable, NewAppTarget, NewAppArgs, (string?)null),
            "UWP / PWA" => (AppTargetKind.Aumid, NewAppTarget, NewAppArgs, (string?)null),
            "URL" => (AppTargetKind.Url, NewAppTarget, NewAppArgs, (string?)null),
            "Browser Profile" => (AppTargetKind.BrowserProfile, NewAppBrowser, NewAppArgs, NewAppProfileDir),
            "Folder" => (AppTargetKind.ShellFolder, NewAppTarget, NewAppArgs, (string?)null),
            _ => (AppTargetKind.Executable, NewAppTarget, NewAppArgs, (string?)null)
        };

        var app = new ProfileApp
        {
            DisplayName = NewAppName,
            Kind = kind,
            Target = target,
            LaunchArgs = args,
            BrowserProfileDir = profileDir,
            SortIndex = Apps.Count
        };
        _profile.Apps.Add(app);
        Apps.Add(new AppRowViewModel(app));

        NewAppName = "";
        NewAppTarget = "";
        NewAppArgs = null;
        StatusText = "App added.";
    }

    /// <summary>
    /// Opens the App Picker dialog and, on success, adds the chosen app to the profile.
    /// This is the user-friendly way to add apps — no need to know AUMIDs or paths.
    /// </summary>
    public void AddPickedApp(PickedApp picked)
    {
        AppTargetKind kind = picked.Source switch
        {
            Core.Launching.AppSourceKind.Uwp     => AppTargetKind.Aumid,
            Core.Launching.AppSourceKind.Win32   => AppTargetKind.Executable,
            Core.Launching.AppSourceKind.Browser => AppTargetKind.BrowserProfile,
            Core.Launching.AppSourceKind.Url     => AppTargetKind.Url,
            Core.Launching.AppSourceKind.Folder  => AppTargetKind.ShellFolder,
            _ => AppTargetKind.Executable
        };
        var app = new ProfileApp
        {
            DisplayName = picked.DisplayName,
            Kind = kind,
            Target = picked.Target,
            LaunchArgs = picked.LaunchArgs,
            BrowserProfileDir = picked.Source == Core.Launching.AppSourceKind.Browser ? "Default" : null,
            SortIndex = Apps.Count
        };
        _profile.Apps.Add(app);
        Apps.Add(new AppRowViewModel(app));
        StatusText = $"Added {picked.DisplayName}";
    }

    [RelayCommand]
    private void RemoveApp(AppRowViewModel? row)
    {
        if (row is null) return;
        _profile.Apps.Remove(row.Source);
        Apps.Remove(row);
        StatusText = "App removed.";
    }

    [RelayCommand]
    private void MoveUp(AppRowViewModel? row)
    {
        if (row is null) return;
        var idx = Apps.IndexOf(row);
        if (idx <= 0) return;
        Apps.Move(idx, idx - 1);
        Reindex();
    }

    [RelayCommand]
    private void MoveDown(AppRowViewModel? row)
    {
        if (row is null) return;
        var idx = Apps.IndexOf(row);
        if (idx < 0 || idx >= Apps.Count - 1) return;
        Apps.Move(idx, idx + 1);
        Reindex();
    }

    private void Reindex()
    {
        for (int i = 0; i < Apps.Count; i++)
        {
            Apps[i].Source.SortIndex = i;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) { StatusText = "Name is required."; return; }
        _profile.Name = Name.Trim();
        _profile.IconGlyph = IconGlyph;
        _profile.AccentColor = AccentColor;
        _profile.DefaultLayout = string.IsNullOrWhiteSpace(DefaultLayout) ? null : DefaultLayout;
        _profile.UpdatedAt = DateTime.UtcNow;
        Reindex();

        try
        {
            await _repo.UpsertAsync(_profile);
            StatusText = IsNew ? "Profile created." : "Profile saved.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to save profile");
            StatusText = $"Save failed: {ex.Message}";
            throw;
        }
    }

    [RelayCommand]
    private void BrowseExe()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Pick an executable",
            Filter = "Executables (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            NewAppTarget = dlg.FileName;
            if (string.IsNullOrWhiteSpace(NewAppName))
                NewAppName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
        }
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        // Use Win32 folder picker via Microsoft.Win32.OpenFolderDialog (net8) or fallback
        try
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Pick a folder"
            };
            if (dlg.ShowDialog() == true)
                NewAppTarget = dlg.FolderName;
        }
        catch
        {
            // Fallback: use SaveFileDialog hack
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Pick any file inside the folder" };
            if (dlg.ShowDialog() == true)
                NewAppTarget = System.IO.Path.GetDirectoryName(dlg.FileName) ?? "";
        }
    }
}

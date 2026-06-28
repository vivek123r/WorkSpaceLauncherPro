using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkSpaceLauncherPro.Core.Launching;
using WorkSpaceLauncherPro.Core.Models;

namespace WorkSpaceLauncherPro.App.ViewModels;

public sealed partial class AppPickerViewModel : ObservableObject
{
    private readonly AppResolver _resolver;
    private List<InstalledApp> _all = new();
    private readonly List<BrowserEntry> _browsers = new()
    {
        new("Chrome", "chrome", @"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"),
        new("Edge",   "edge",   @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"),
        new("Brave",  "brave",  @"%LOCALAPPDATA%\BraveSoftware\Brave-Browser\Application\brave.exe"),
        new("Opera",  "opera",  @"%USERPROFILE%\AppData\Local\Programs\Opera GX\opera.exe"),
    };
    private readonly List<string> _urlSuggestions = new()
    {
        "https://github.com",
        "https://stackoverflow.com",
        "https://mail.google.com",
        "https://calendar.google.com"
    };
    private readonly List<string> _folders = new()
    {
        @"C:\Users\vivek\Downloads",
        @"C:\Users\vivek\Documents",
        @"C:\Users\vivek\Desktop"
    };

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _statusText = "Loading installed apps…";

    [ObservableProperty]
    private InstalledApp? _selectedApp;

    [ObservableProperty]
    private BrowserEntry? _selectedBrowser;

    [ObservableProperty]
    private string _newUrl = "https://";

    [ObservableProperty]
    private string _newFolder = "";

    public ObservableCollection<InstalledApp> FilteredApps { get; } = new();
    public ObservableCollection<BrowserEntry> FilteredBrowsers { get; } = new();

    public System.Collections.ObjectModel.ReadOnlyCollection<string> UrlSuggestions => _urlSuggestions.AsReadOnly();
    public System.Collections.ObjectModel.ReadOnlyCollection<string> FolderSuggestions => _folders.AsReadOnly();

    public bool IsBrowserTab => SelectedTabIndex == 3;
    public bool IsUrlTab => SelectedTabIndex == 4;
    public bool IsFolderTab => SelectedTabIndex == 5;

    public AppPickerViewModel(AppResolver resolver)
    {
        _resolver = resolver;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsBrowserTab));
        OnPropertyChanged(nameof(IsUrlTab));
        OnPropertyChanged(nameof(IsFolderTab));
        ApplyFilter();
    }

    public async Task LoadAsync()
    {
        try
        {
            _all = (await Task.Run(() => _resolver.EnumerateAllInstalledApps())).ToList();
            StatusText = $"{_all.Count} apps found · {_browsers.Count} browsers";
        }
        catch (Exception ex)
        {
            StatusText = $"Discovery failed: {ex.Message}";
        }
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredApps.Clear();
        FilteredBrowsers.Clear();

        var tab = SelectedTabIndex; // 0=All, 1=Win32, 2=UWP, 3=Browser, 4=URL, 5=Folder
        var q = (SearchText ?? "").Trim();

        if (tab is 0 or 1 or 2)
        {
            foreach (var app in _all)
            {
                if (tab == 1 && app.Source != AppSourceKind.Win32) continue;
                if (tab == 2 && app.Source != AppSourceKind.Uwp) continue;
                if (q.Length > 0 &&
                    app.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (app.AumId?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
                    continue;
                FilteredApps.Add(app);
            }
        }

        if (tab == 0 || tab == 3)
        {
            foreach (var b in _browsers)
            {
                if (q.Length > 0 && b.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                FilteredBrowsers.Add(b);
            }
        }
    }
}

public sealed record BrowserEntry(string DisplayName, string BrowserKey, string DefaultExePath);

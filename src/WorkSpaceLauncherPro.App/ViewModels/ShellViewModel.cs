using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.App.Services;
using WorkSpaceLauncherPro.App.Views;
using WorkSpaceLauncherPro.Data.Repositories;
using WorkSpaceLauncherPro.Layout;

namespace WorkSpaceLauncherPro.App.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IProfileRepository _profiles;
    private readonly LayoutEngine _layoutEngine;
    private readonly ILogger<ShellViewModel> _log;
    private readonly ITrayService _tray;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ProfileCardViewModel> Profiles { get; } = new();

    public ShellViewModel(IProfileRepository profiles, LayoutEngine layoutEngine, ILogger<ShellViewModel> log, ITrayService tray)
    {
        _profiles = profiles;
        _layoutEngine = layoutEngine;
        _log = log;
        _tray = tray;
    }

    public async Task LoadAsync()
    {
        try
        {
            var all = await _profiles.GetAllAsync();
            Profiles.Clear();
            foreach (var p in all)
            {
                var card = new ProfileCardViewModel(p, _layoutEngine, _log);
                card.EditRequested += OnEditRequested;
                card.DeleteRequested += OnDeleteRequested;
                Profiles.Add(card);
            }
            StatusText = $"{Profiles.Count} profile(s) loaded.";
            _tray.RebuildMenu();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load profiles");
            StatusText = "Failed to load profiles — see logs.";
        }
    }

    private void OnEditRequested(object? sender, EventArgs e)
    {
        if (sender is not ProfileCardViewModel card) return;
        var editor = App.Services.GetRequiredService<ProfileEditorWindow>();
        editor.DataContext = new ProfileEditorViewModel(
            card.Profile,
            App.Services.GetRequiredService<IProfileRepository>(),
            App.Services.GetRequiredService<ILogger<ProfileEditorViewModel>>());
        if (editor.ShowDialog() == true)
            _ = LoadAsync();
    }

    private async void OnDeleteRequested(object? sender, EventArgs e)
    {
        if (sender is not ProfileCardViewModel card) return;
        var result = MessageBox.Show(
            $"Delete profile \"{card.Name}\"? This can't be undone.",
            "WorkSpace Launcher Pro",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _profiles.DeleteAsync(card.Id);
            StatusText = $"Deleted {card.Name}";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to delete profile {Id}", card.Id);
            StatusText = $"Delete failed: {ex.Message}";
        }
    }
}

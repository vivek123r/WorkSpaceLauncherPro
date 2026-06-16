using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.App.Services;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Layout;

namespace WorkSpaceLauncherPro.App.ViewModels;

public sealed partial class ProfileCardViewModel : ObservableObject
{
    private readonly Profile _profile;
    private readonly LayoutEngine _layoutEngine;
    private readonly ILogger _log;

    public Guid Id => _profile.Id;
    public Profile Profile => _profile;
    public string Name => _profile.Name;
    public string IconGlyph => _profile.IconGlyph;
    public string Subtitle => _profile.Apps.Count == 0
        ? "Empty"
        : $"{_profile.Apps.Count} app(s)";
    public Brush AccentBrush => new SolidColorBrush(_profile.AccentColor);

    public IAsyncRelayCommand LaunchCommand { get; }
    public IRelayCommand EditCommand { get; }
    public IRelayCommand DeleteCommand { get; }

    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;

    public ProfileCardViewModel(Profile profile, LayoutEngine layoutEngine, ILogger log)
    {
        _profile = profile;
        _layoutEngine = layoutEngine;
        _log = log;
        LaunchCommand = new AsyncRelayCommand(LaunchAsync);
        EditCommand = new RelayCommand(() => EditRequested?.Invoke(this, EventArgs.Empty));
        DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));
    }

    private async Task LaunchAsync()
    {
        _log.LogInformation("Launch requested for profile: {Name}", Name);
        var launcher = App.Services.GetRequiredService<ILauncherService>();
        await launcher.LaunchWithDefaultLayoutAsync(_profile, CancellationToken.None);
    }
}

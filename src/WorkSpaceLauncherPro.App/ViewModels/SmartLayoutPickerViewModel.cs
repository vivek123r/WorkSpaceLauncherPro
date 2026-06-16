using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;
using WorkSpaceLauncherPro.Layout;
using WorkSpaceLauncherPro.Layout.Templates;

namespace WorkSpaceLauncherPro.App.ViewModels;

public sealed partial class LayoutOptionViewModel : ObservableObject
{
    public LayoutTemplate Template { get; }
    public string Name => Template.Name;
    public string Description => Template.Description;
    public string IconGlyph => Template.IconGlyph;
    public IReadOnlyList<LayoutRect> Rects { get; }
    public Profile Profile { get; }

    public LayoutOptionViewModel(LayoutTemplate template, Profile profile, IReadOnlyList<LayoutRect> rects)
    {
        Template = template;
        Profile = profile;
        Rects = rects;
    }
}

public sealed partial class SmartLayoutPickerViewModel : ObservableObject
{
    private readonly LayoutEngine _engine;
    private readonly IMonitorEnumerator _monitors;
    private readonly ILogger<SmartLayoutPickerViewModel> _log;
    private readonly Profile _profile;
    private MonitorInfo? _monitor;

    [ObservableProperty]
    private string _statusText = "Pick a layout to launch with.";

    [ObservableProperty]
    private LayoutOptionViewModel? _selected;

    public ObservableCollection<LayoutOptionViewModel> Layouts { get; } = new();

    public Profile Profile => _profile;
    public string WindowCount => _profile.Apps.Count == 0 ? "0 apps" : $"{_profile.Apps.Count} apps";
    public string MonitorLabel => _monitor is { } m
        ? $"Monitor: {m.DeviceName}  ·  {m.WorkingAreaPhysical.Width}×{m.WorkingAreaPhysical.Height}"
        : "No monitor detected";

    public SmartLayoutPickerViewModel(Profile profile, LayoutEngine engine, IMonitorEnumerator monitors, ILogger<SmartLayoutPickerViewModel> log)
    {
        _profile = profile;
        _engine = engine;
        _monitors = monitors;
        _log = log;
        LoadLayouts();
    }

    private void LoadLayouts()
    {
        var all = _monitors.Current();
        _monitor = all.FirstOrDefault(m => m.IsPrimary) ?? all.FirstOrDefault();
        if (_monitor is null)
        {
            StatusText = "No monitor detected — can't compute layouts.";
            return;
        }
        OnPropertyChanged(nameof(MonitorLabel));

        Layouts.Clear();
        var suggested = _engine.SuggestLayouts(_profile.Apps.Count, _monitor);
        foreach (var tmpl in suggested)
        {
            try
            {
                var rects = tmpl.Generate(_profile.Apps.Count, _monitor);
                Layouts.Add(new LayoutOptionViewModel(tmpl, _profile, rects));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Template {Name} failed to generate for n={N}", tmpl.Name, _profile.Apps.Count);
            }
        }

        Selected = Layouts.FirstOrDefault();
        StatusText = $"{Layouts.Count} layout(s) available. Click one to launch.";
        OnPropertyChanged(nameof(WindowCount));
    }

    [RelayCommand]
    private void Shuffle()
    {
        if (_monitor is null) return;
        var t = _engine.WithShuffledHero(
            Layouts.FirstOrDefault()?.Template
                ?? new HeroPlusGridTemplate(_profile.Apps.Count, 0),
            _profile.Apps.Count);
        try
        {
            var rects = t.Generate(_profile.Apps.Count, _monitor);
            Layouts.Insert(0, new LayoutOptionViewModel(t, _profile, rects));
            Selected = Layouts[0];
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Shuffled template failed");
        }
    }
}

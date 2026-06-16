using System.Drawing;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Data.Repositories;

namespace WorkSpaceLauncherPro.App.Services;

/// <summary>
/// System tray icon + context menu. Implemented with WinForms NotifyIcon
/// (we enable &lt;UseWindowsForms&gt;true&lt;/UseWindowsForms&gt; in the App csproj).
/// </summary>
public interface ITrayService : IDisposable
{
    void Start();
    void RebuildMenu();
}

public sealed class TrayService : ITrayService
{
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu;
    private readonly IServiceProvider _services;
    private readonly IProfileRepository _repo;
    private readonly ILogger<TrayService> _log;

    public TrayService(IServiceProvider services, IProfileRepository repo, ILogger<TrayService> log)
    {
        _services = services;
        _repo = repo;
        _log = log;

        _menu = new ContextMenuStrip();
        _icon = new NotifyIcon
        {
            Text = "WorkSpace Launcher Pro",
            Icon = LoadIcon(),
            Visible = true,
            ContextMenuStrip = _menu
        };
        _icon.DoubleClick += (_, _) => ShowShell();
    }

    public void Start() => RebuildMenu();

    public void RebuildMenu()
    {
        if (Application.Current?.Dispatcher is { } d && !d.CheckAccess())
        {
            d.Invoke(RebuildMenu);
            return;
        }

        _menu.Items.Clear();
        _menu.Items.Add("Open WorkSpace Launcher", null, (_, _) => ShowShell());

        _menu.Items.Add(new ToolStripSeparator());

        // Per-profile quick-launch
        ToolStripMenuItem profilesItem = new("Quick launch")
        {
            Enabled = false
        };
        _menu.Items.Add(profilesItem);

        try
        {
            var all = _repo.GetAllAsync().GetAwaiter().GetResult();
            if (all.Count == 0)
            {
                profilesItem.Text = "(no profiles yet)";
            }
            else
            {
                profilesItem.Enabled = true;
                profilesItem.DropDownItems.Clear();
                foreach (var p in all.OrderBy(x => x.SortIndex).ThenBy(x => x.Name))
                {
                    var item = new ToolStripMenuItem($"{p.IconGlyph}  {p.Name}")
                    {
                        Tag = p
                    };
                    item.Click += (_, _) => LaunchProfile(p.Id);
                    profilesItem.DropDownItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to build tray menu");
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Quit", null, (_, _) =>
        {
            if (Application.Current is not null) Application.Current.Shutdown();
        });
    }

    private void ShowShell()
    {
        if (Application.Current?.MainWindow is { } win)
        {
            if (!win.IsVisible) win.Show();
            if (win.WindowState == WindowState.Minimized) win.WindowState = WindowState.Normal;
            win.Activate();
        }
    }

    private void LaunchProfile(Guid id)
    {
        try
        {
            var shell = _services.GetRequiredService<WorkSpaceLauncherPro.App.Views.ShellWindow>();
            if (Application.Current?.MainWindow is null)
            {
                shell.Show();
            }
            shell.Activate();
            // For now: trigger via tray "open shell" — user can click the card.
            // A future improvement: directly fire the card's launch.
            _ = _repo.GetByIdAsync(id).ContinueWith(t =>
            {
                if (t.Result is { } p)
                {
                    var launcher = _services.GetRequiredService<ILauncherService>();
                    _ = launcher.LaunchWithDefaultLayoutAsync(p);
                }
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Tray launch failed");
        }
    }

    private static Icon LoadIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "app.ico");
            if (File.Exists(path)) return new Icon(path);
        }
        catch { /* fall through */ }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }
}

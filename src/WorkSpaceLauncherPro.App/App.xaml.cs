using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.App.Bootstrap;
using WorkSpaceLauncherPro.App.Services;
using WorkSpaceLauncherPro.App.ViewModels;
using WorkSpaceLauncherPro.App.Views;
using WorkSpaceLauncherPro.Data;

namespace WorkSpaceLauncherPro.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private MutexGuard? _mutex;
    private ILogger<App>? _log;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // 1. Single-instance check
        _mutex = new MutexGuard("Global\\WorkSpaceLauncherPro.SingleInstance");
        if (!_mutex.Acquire())
        {
            _mutex.NotifyFirstInstance();
            Shutdown();
            return;
        }

        // 2. Parse CLI args
        var args = CliArgs.Parse(e.Args);
        var startInTray = args.Has("tray") || args.Has("silent");

        // 3. Build DI container
        Services = new ServiceCollection()
            .AddWorkSpaceServices()
            .BuildServiceProvider();

        _log = Services.GetRequiredService<ILogger<App>>();
        _log.LogInformation("WorkSpace Launcher Pro starting. Args: {Args}", string.Join(" ", e.Args));

        // 4. Install global exception handlers
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // 5. Initialize database (run migrations)
        try
        {
            var dbInit = Services.GetRequiredService<DatabaseInitializer>();
            dbInit.Initialize();
        }
        catch (Exception ex)
        {
            _log.LogCritical(ex, "Database initialization failed");
            MessageBox.Show($"Database initialization failed: {ex.Message}", "WorkSpace Launcher Pro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // 6. Show main window (or start in tray)
        var main = Services.GetRequiredService<ShellWindow>();
        main.DataContext = Services.GetRequiredService<ShellViewModel>();

        if (!startInTray)
        {
            main.Show();
        }
        else
        {
            // Auto-minimize to tray
            main.Hide();
            _log.LogInformation("Started in tray mode (--tray arg).");
        }

        // 7. Start system tray
        Services.GetRequiredService<ITrayService>().Start();

        // 8. Register global hotkeys
        Services.GetRequiredService<IHotkeyService>().Start(main);
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _mutex?.Dispose();
        Services.GetRequiredService<ITrayService>()?.Dispose();
        Services.GetRequiredService<IHotkeyService>()?.Dispose();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log?.LogError(e.Exception, "Unhandled dispatcher exception");
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _log?.LogError(e.ExceptionObject as Exception, "Unhandled domain exception");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _log?.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}

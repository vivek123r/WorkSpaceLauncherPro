using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.App.Services;
using WorkSpaceLauncherPro.App.ViewModels;
using WorkSpaceLauncherPro.App.Views;
using WorkSpaceLauncherPro.Core.Services;
using WorkSpaceLauncherPro.Core.Windowing;
using WorkSpaceLauncherPro.Core.Launching;
using WorkSpaceLauncherPro.Data;
using WorkSpaceLauncherPro.Data.Migrations;
using WorkSpaceLauncherPro.Data.Repositories;
using WorkSpaceLauncherPro.Layout;

namespace WorkSpaceLauncherPro.App.Bootstrap;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkSpaceServices(this IServiceCollection services)
    {
        // === Logging ===
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkSpaceLauncherPro");
        Directory.CreateDirectory(appDataPath);
        var logPath = Path.Combine(appDataPath, "logs");
        Directory.CreateDirectory(logPath);

        services.AddLogging(b => b
            .SetMinimumLevel(LogLevel.Information)
            .AddDebug()
            .AddProvider(new FileLoggerProvider(logPath)));

        // === Data layer ===
        var dbPath = Path.Combine(appDataPath, "db.sqlite");
        var connectionString = $"Data Source={dbPath};Cache=Shared;Pooling=True";
        services.AddSingleton<IDbConnectionFactory>(_ => new SqliteConnectionFactory(connectionString));
        services.AddSingleton<MigrationRunner>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<IProfileRepository, ProfileRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IMonitorSnapshotRepository, MonitorSnapshotRepository>();

        // === Core / platform ===
        services.AddSingleton<MonitorEnumerator>();
        services.AddSingleton<IMonitorEnumerator>(sp => sp.GetRequiredService<MonitorEnumerator>());
        services.AddSingleton<WindowEnumerator>();
        services.AddSingleton<WindowPositionEngine>();
        services.AddSingleton<DpiHelper>();
        services.AddSingleton<PwaLauncher>();
        services.AddSingleton<BrowserLauncher>();
        services.AddSingleton<AppResolver>();

        // === Domain services ===
        services.AddSingleton<ILauncherService, LauncherService>();
        services.AddSingleton<ProfileService>();
        services.AddSingleton<StartupService>();
        services.AddSingleton<PowerShellImportService>();

        // === Layout engine ===
        services.AddSingleton<LayoutTemplateRegistry>();
        services.AddSingleton<LayoutEngine>();
        services.AddSingleton<LiveSwapController>();

        // === App services ===
        services.AddSingleton<IServiceProvider>(sp => sp);
        services.AddSingleton<ITrayService, TrayService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();

        // === ViewModels ===
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<ProfileEditorViewModel>();
        services.AddTransient<VisualDesignerViewModel>();
        services.AddTransient<SmartLayoutPickerViewModel>();
        services.AddTransient<MagicArrangeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ImportViewModel>();

        // === Views ===
        services.AddSingleton<ShellWindow>();
        services.AddTransient<ProfileEditorWindow>();
        services.AddTransient<VisualDesignerWindow>();
        services.AddTransient<SmartLayoutPickerWindow>();
        services.AddTransient<MagicArrangeOverlay>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<ImportWindow>();
        services.AddTransient<AppPickerWindow>();
        services.AddTransient<AppPickerViewModel>();

        return services;
    }
}

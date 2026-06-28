using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WorkSpaceLauncherPro.App.ViewModels;
using AppSourceKind = WorkSpaceLauncherPro.Core.Launching.AppSourceKind;

namespace WorkSpaceLauncherPro.App.Views;

public partial class AppPickerWindow : Window
{
    /// <summary>The result the user picked. Null on cancel.</summary>
    public PickedApp? Result { get; private set; }

    public AppPickerWindow() => InitializeComponent();

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AppPickerViewModel vm) return;

        // Tab indices: 0=All, 1=Win32, 2=UWP, 3=Browser, 4=URL, 5=Folder
        PickedApp? picked = vm.SelectedTabIndex switch
        {
            3 => vm.SelectedBrowser is { } b
                ? new PickedApp(AppSourceKind.Browser, b.BrowserKey, b.DisplayName, null, null, null)
                : null,
            4 => string.IsNullOrWhiteSpace(vm.NewUrl) || vm.NewUrl == "https://"
                ? null
                : new PickedApp(AppSourceKind.Url, vm.NewUrl.Trim(), vm.NewUrl.Trim(), null, null, null),
            5 => string.IsNullOrWhiteSpace(vm.NewFolder)
                ? null
                : new PickedApp(AppSourceKind.Folder, vm.NewFolder.Trim(),
                    System.IO.Path.GetFileName(vm.NewFolder.Trim()), null, null, null),
            _ => vm.SelectedApp is { } a
                ? new PickedApp(
                    a.Source == AppSourceKind.Uwp ? AppSourceKind.Uwp : AppSourceKind.Win32,
                    a.AumId, a.DisplayName, a.InstallPath, a.AumId, a.LaunchArgs)
                : null
        };

        if (picked is null)
        {
            MessageBox.Show("Pick something first.", "Add Application",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Result = picked;
        DialogResult = true;
        Close();
    }

    private void OnUrlSuggestionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string url } && DataContext is AppPickerViewModel vm)
            vm.NewUrl = url;
    }

    private void OnFolderSuggestionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Content: string folder } && DataContext is AppPickerViewModel vm)
            vm.NewFolder = folder;
    }

    private void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Pick a folder" };
            if (dlg.ShowDialog() == true && DataContext is AppPickerViewModel vm)
                vm.NewFolder = dlg.FolderName;
        }
        catch
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Pick any file inside the folder" };
            if (dlg.ShowDialog() == true && DataContext is AppPickerViewModel vm)
                vm.NewFolder = System.IO.Path.GetDirectoryName(dlg.FileName) ?? "";
        }
    }

    private void OnAppDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            OnAddClick(sender, new RoutedEventArgs());
    }
}

public sealed record PickedApp(
    AppSourceKind Source,
    string Target,
    string DisplayName,
    string? ExePath,
    string? AumId,
    string? LaunchArgs);

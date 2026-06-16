using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkSpaceLauncherPro.App.ViewModels;

namespace WorkSpaceLauncherPro.App.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ShellViewModel vm) await vm.LoadAsync();
        };
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnAddProfileClick(object sender, RoutedEventArgs e)
    {
        var editor = App.Services.GetRequiredService<ProfileEditorWindow>();
        editor.DataContext = new ProfileEditorViewModel();
        if (editor.ShowDialog() == true)
        {
            if (DataContext is ShellViewModel vm) _ = vm.LoadAsync();
        }
    }

    private void OnOpenImportClick(object sender, RoutedEventArgs e)
    {
        var win = App.Services.GetRequiredService<ImportWindow>();
        if (win.ShowDialog() == true)
        {
            if (DataContext is ShellViewModel vm) _ = vm.LoadAsync();
        }
    }
}

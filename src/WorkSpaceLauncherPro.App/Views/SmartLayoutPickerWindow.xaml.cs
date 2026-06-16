using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkSpaceLauncherPro.App.Services;
using WorkSpaceLauncherPro.App.ViewModels;

namespace WorkSpaceLauncherPro.App.Views;

public partial class SmartLayoutPickerWindow : Window
{
    public LaunchResult? Result { get; private set; }

    public SmartLayoutPickerWindow() => InitializeComponent();

    private void OnCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is LayoutOptionViewModel opt)
        {
            if (DataContext is SmartLayoutPickerViewModel vm)
                vm.Selected = opt;
        }
    }

    private async void OnLaunchClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SmartLayoutPickerViewModel vm) return;
        var layout = vm.Selected?.Template;
        if (layout is null)
        {
            MessageBox.Show("Pick a layout first.", "Smart Layout Picker",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            IsEnabled = false;
            vm.StatusText = "Launching…";
            Mouse.OverrideCursor = Cursors.Wait;
            var launcher = App.Services.GetRequiredService<ILauncherService>();
            var report = await launcher.LaunchWithLayoutAsync(vm.Profile, layout);
            Result = new LaunchResult(report.Requested, report.Placed, report.Failed, report.Elapsed);
            vm.StatusText = $"Launched {Result.Placed}/{Result.Requested} (failed: {Result.Failed}) in {Result.Elapsed.TotalSeconds:F1}s";
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Launch failed: {ex.Message}", "WorkSpace Launcher Pro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public sealed record LaunchResult(int Requested, int Placed, int Failed, TimeSpan Elapsed);

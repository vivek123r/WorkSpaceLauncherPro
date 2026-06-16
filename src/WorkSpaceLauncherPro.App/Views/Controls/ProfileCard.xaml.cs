using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkSpaceLauncherPro.App.ViewModels;

namespace WorkSpaceLauncherPro.App.Views.Controls;

public partial class ProfileCard : Button
{
    public ProfileCard() => InitializeComponent();

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProfileCardViewModel card) return;
        if (card.Profile.Apps.Count == 0)
        {
            MessageBox.Show("This profile is empty. Add some apps first.",
                "WorkSpace Launcher Pro", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Open Smart Layout Picker
        var picker = App.Services.GetRequiredService<SmartLayoutPickerWindow>();
        picker.DataContext = new SmartLayoutPickerViewModel(
            card.Profile,
            App.Services.GetRequiredService<WorkSpaceLauncherPro.Layout.LayoutEngine>(),
            App.Services.GetRequiredService<WorkSpaceLauncherPro.Core.Windowing.IMonitorEnumerator>(),
            App.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmartLayoutPickerViewModel>>());
        picker.ShowDialog();
    }
}

using System.Windows;
using WorkSpaceLauncherPro.App.ViewModels;

namespace WorkSpaceLauncherPro.App.Views;

public partial class ProfileEditorWindow : Window
{
    public ProfileEditorWindow() => InitializeComponent();

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProfileEditorViewModel vm)
        {
            try
            {
                await vm.SaveCommand.ExecuteAsync(null);
                if (vm.StatusText.StartsWith("Save failed", StringComparison.Ordinal))
                {
                    // Don't close; show error
                    return;
                }
                DialogResult = true;
                Close();
            }
            catch
            {
                // status text already set
            }
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

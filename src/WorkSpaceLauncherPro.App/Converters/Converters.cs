using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WorkSpaceLauncherPro.App.ViewModels;

public sealed class BoolToTextConverter : IValueConverter
{
    public static readonly BoolToTextConverter NewOrEdit = new() { TrueValue = "New Profile", FalseValue = "Edit Profile" };
    public string TrueValue { get; set; } = "Yes";
    public string FalseValue { get; set; } = "No";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? TrueValue : FalseValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StringEqualsConverter : IValueConverter
{
    public static readonly StringEqualsConverter BrowserVis = new() { Match = "Browser Profile", Result = Visibility.Visible };
    public static readonly StringEqualsConverter ExeVis = new() { Match = "Executable", Result = Visibility.Visible };
    public static readonly StringEqualsConverter FolderVis = new() { Match = "Folder", Result = Visibility.Visible };

    public string Match { get; set; } = "";
    public Visibility Result { get; set; } = Visibility.Visible;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && s == Match ? Result : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

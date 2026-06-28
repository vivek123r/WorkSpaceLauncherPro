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

    // Picker tab visibility — true when value (selected tab) is in MatchList
    public static readonly StringEqualsConverter AppListVis = new() { Match = "All", Result = Visibility.Visible };
    public static readonly StringEqualsConverter Win32Vis = new() { Match = "Win32", Result = Visibility.Visible };
    public static readonly StringEqualsConverter UrlVis = new() { Match = "URL", Result = Visibility.Visible };

    public string Match { get; set; } = "";
    public Visibility Result { get; set; } = Visibility.Visible;
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool eq = value is string s && s == Match;
        if (Invert) eq = !eq;
        return eq ? Result : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// For RadioButton: maps the button's string (passed as parameter) to IsChecked
/// based on whether it matches the bound string. Used by tab strip.
/// </summary>
public sealed class StringEqualsRadioConverter : IValueConverter
{
    public static readonly StringEqualsRadioConverter Instance = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && parameter is string p && s == p;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? parameter : System.Windows.Data.Binding.DoNothing;
}

/// <summary>True → Visible, false → Collapsed. Referenceable as {StaticResource BoolToVis}.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

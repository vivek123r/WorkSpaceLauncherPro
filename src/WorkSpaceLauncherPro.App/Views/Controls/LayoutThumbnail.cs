using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.App.ViewModels;

namespace WorkSpaceLauncherPro.App.Views.Controls;

/// <summary>
/// Renders a list of <see cref="LayoutRect"/> as a scaled thumbnail inside a Canvas.
/// Coordinates are normalized to 0..1 then mapped to the available size.
/// </summary>
public sealed class LayoutThumbnail : ContentControl
{
    public static readonly DependencyProperty RectsProperty = DependencyProperty.Register(
        nameof(Rects), typeof(IReadOnlyList<LayoutRect>), typeof(LayoutThumbnail),
        new PropertyMetadata(null, OnRectsChanged));

    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Color), typeof(LayoutThumbnail),
        new PropertyMetadata(Colors.DodgerBlue, OnRectsChanged));

    public IReadOnlyList<LayoutRect>? Rects
    {
        get => (IReadOnlyList<LayoutRect>?)GetValue(RectsProperty);
        set => SetValue(RectsProperty, value);
    }

    public Color Accent
    {
        get => (Color)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    private readonly Canvas _canvas = new() { Background = Brushes.Transparent };

    public LayoutThumbnail()
    {
        Content = _canvas;
        SizeChanged += (_, _) => Render();
        // Default style: corner radius, subtle background
        Template = null;
    }

    private static void OnRectsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((LayoutThumbnail)d).Render();

    private void Render()
    {
        _canvas.Children.Clear();
        if (Rects is null || Rects.Count == 0) return;

        // Normalize to monitor space first
        int maxX = Rects.Max(r => r.X + r.Width);
        int maxY = Rects.Max(r => r.Y + r.Height);
        if (maxX <= 0 || maxY <= 0) return;

        double w = ActualWidth - Padding.Left - Padding.Right;
        double h = ActualHeight - Padding.Top - Padding.Bottom;
        if (w <= 0 || h <= 0) return;

        // Aspect-fit
        double scale = Math.Min(w / maxX, h / maxY);
        double offX = Padding.Left + (w - maxX * scale) / 2;
        double offY = Padding.Top + (h - maxY * scale) / 2;

        var baseFill = new SolidColorBrush(Accent) { Opacity = 0.55 };
        var borderPen = new SolidColorBrush(Accent) { Opacity = 0.95 };
        var highlight = new SolidColorBrush(Colors.White) { Opacity = 0.85 };

        for (int i = 0; i < Rects.Count; i++)
        {
            var r = Rects[i];
            var rect = new Rectangle
            {
                Width = r.Width * scale,
                Height = r.Height * scale,
                Fill = baseFill,
                Stroke = borderPen,
                StrokeThickness = 1.2,
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(rect, offX + r.X * scale);
            Canvas.SetTop(rect, offY + r.Y * scale);
            _canvas.Children.Add(rect);

            // Slot number label
            if (r.Width * scale > 24 && r.Height * scale > 16)
            {
                var lbl = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    Foreground = highlight,
                    FontSize = Math.Max(9, Math.Min(13, r.Width * scale / 5)),
                    FontWeight = FontWeights.SemiBold
                };
                Canvas.SetLeft(lbl, offX + r.X * scale + 4);
                Canvas.SetTop(lbl, offY + r.Y * scale + 2);
                _canvas.Children.Add(lbl);
            }
        }
    }
}

using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Layout.Templates;

/// <summary>50/50 horizontal split.</summary>
public sealed class HalvesTemplate : LayoutTemplate
{
    public override string Name => "Side by Side";
    public override string Description => "50/50 horizontal split — two windows share the screen equally.";
    public override string IconGlyph => "▭▭";
    public override bool Supports(int n) => n == 2;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var w = m.WorkingAreaPhysical.Width / 2;
        var h = m.WorkingAreaPhysical.Height;
        return new[]
        {
            new LayoutRect(0, 0, 0, w, h, m.Index),
            new LayoutRect(1, w, 0, m.WorkingAreaPhysical.Width - w, h, m.Index)
        };
    }
}

public sealed class VerticalStackTemplate : LayoutTemplate
{
    public override string Name => "Stacked";
    public override string Description => "50/50 vertical split — two windows stacked top/bottom.";
    public override string IconGlyph => "▭\n▭";
    public override bool Supports(int n) => n == 2;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var h = m.WorkingAreaPhysical.Height / 2;
        var w = m.WorkingAreaPhysical.Width;
        return new[]
        {
            new LayoutRect(0, 0, 0, w, h, m.Index),
            new LayoutRect(1, 0, h, w, m.WorkingAreaPhysical.Height - h, m.Index)
        };
    }
}

public sealed class MainSideTemplate : LayoutTemplate
{
    public readonly int HeroIndex;
    public readonly double HeroRatio;

    public MainSideTemplate(int heroIndex = 0, double heroRatio = 0.70)
    {
        HeroIndex = heroIndex; HeroRatio = heroRatio;
    }

    public override string Name => $"Main + Side ({(int)(HeroRatio * 100)}/{100 - (int)(HeroRatio * 100)})";
    public override string Description => "1 large hero window + 1 smaller sidebar window.";
    public override string IconGlyph => "▭▭";
    public override bool Supports(int n) => n == 2;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var heroW = (int)(m.WorkingAreaPhysical.Width * HeroRatio);
        var sideW = m.WorkingAreaPhysical.Width - heroW;
        var hero = new LayoutRect(HeroIndex, 0, 0, heroW, m.WorkingAreaPhysical.Height, m.Index);
        var side = new LayoutRect(1 - HeroIndex, heroW, 0, sideW, m.WorkingAreaPhysical.Height, m.Index);
        return new[] { hero, side };
    }
}

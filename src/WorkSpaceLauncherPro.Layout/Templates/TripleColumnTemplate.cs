using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Layout.Templates;

public sealed class TripleColumnTemplate : LayoutTemplate
{
    public override string Name => "Triple Column";
    public override string Description => "Three equal vertical columns.";
    public override string IconGlyph => "▮▮▮";
    public override bool Supports(int n) => n == 3;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var w = m.WorkingAreaPhysical.Width / 3;
        return new[]
        {
            new LayoutRect(0, 0, 0, w, m.WorkingAreaPhysical.Height, m.Index),
            new LayoutRect(1, w, 0, w, m.WorkingAreaPhysical.Height, m.Index),
            new LayoutRect(2, 2 * w, 0, m.WorkingAreaPhysical.Width - 2 * w, m.WorkingAreaPhysical.Height, m.Index)
        };
    }
}

public sealed class ThirdsTemplate : LayoutTemplate
{
    public override string Name => "Thirds (rows)";
    public override string Description => "Three equal horizontal rows.";
    public override string IconGlyph => "▬\n▬\n▬";
    public override bool Supports(int n) => n == 3;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var h = m.WorkingAreaPhysical.Height / 3;
        return new[]
        {
            new LayoutRect(0, 0, 0, m.WorkingAreaPhysical.Width, h, m.Index),
            new LayoutRect(1, 0, h, m.WorkingAreaPhysical.Width, h, m.Index),
            new LayoutRect(2, 0, 2 * h, m.WorkingAreaPhysical.Width, m.WorkingAreaPhysical.Height - 2 * h, m.Index)
        };
    }
}

public sealed class MainPlusTwoStackTemplate : LayoutTemplate
{
    public readonly int HeroIndex;
    public MainPlusTwoStackTemplate(int heroIndex = 0) => HeroIndex = heroIndex;

    public override string Name => $"Main + 2-Stack (hero #{HeroIndex + 1})";
    public override string Description => "1 large hero on the left + 2 stacked on the right.";
    public override string IconGlyph => "▮▮";
    public override bool Supports(int n) => n == 3;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var heroW = (int)(m.WorkingAreaPhysical.Width * 0.6);
        var sideW = m.WorkingAreaPhysical.Width - heroW;
        var halfH = m.WorkingAreaPhysical.Height / 2;
        var hero = new LayoutRect(HeroIndex, 0, 0, heroW, m.WorkingAreaPhysical.Height, m.Index);
        var others = Enumerable.Range(0, 3).Where(i => i != HeroIndex).ToArray();
        return new[]
        {
            hero,
            new LayoutRect(others[0], heroW, 0, sideW, halfH, m.Index),
            new LayoutRect(others[1], heroW, halfH, sideW, m.WorkingAreaPhysical.Height - halfH, m.Index)
        };
    }
}

public sealed class FocusPlusTwoBottomTemplate : LayoutTemplate
{
    public readonly int HeroIndex;
    public FocusPlusTwoBottomTemplate(int heroIndex = 0) => HeroIndex = heroIndex;

    public override string Name => $"Focus + 2-Bottom (hero #{HeroIndex + 1})";
    public override string Description => "1 large hero on top + 2 smaller on the bottom row.";
    public override string IconGlyph => "▬\n▬▬";
    public override bool Supports(int n) => n == 3;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var heroH = (int)(m.WorkingAreaPhysical.Height * 0.65);
        var botH = m.WorkingAreaPhysical.Height - heroH;
        var halfW = m.WorkingAreaPhysical.Width / 2;
        var hero = new LayoutRect(HeroIndex, 0, 0, m.WorkingAreaPhysical.Width, heroH, m.Index);
        var others = Enumerable.Range(0, 3).Where(i => i != HeroIndex).ToArray();
        return new[]
        {
            hero,
            new LayoutRect(others[0], 0, heroH, halfW, botH, m.Index),
            new LayoutRect(others[1], halfW, heroH, m.WorkingAreaPhysical.Width - halfW, botH, m.Index)
        };
    }
}

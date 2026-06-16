using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Layout.Templates;

public sealed class MainPlusThreeStackTemplate : LayoutTemplate
{
    public readonly int HeroIndex;
    public MainPlusThreeStackTemplate(int heroIndex = 0) => HeroIndex = heroIndex;

    public override string Name => $"Main + 3-Stack (hero #{HeroIndex + 1})";
    public override string Description => "1 large hero on the left + 3 stacked on the right.";
    public override string IconGlyph => "▮▮\n▮▮";
    public override bool Supports(int n) => n == 4;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var heroW = (int)(m.WorkingAreaPhysical.Width * 0.6);
        var sideW = m.WorkingAreaPhysical.Width - heroW;
        var rowH = m.WorkingAreaPhysical.Height / 3;
        var hero = new LayoutRect(HeroIndex, 0, 0, heroW, m.WorkingAreaPhysical.Height, m.Index);
        var others = Enumerable.Range(0, 4).Where(i => i != HeroIndex).ToArray();
        return new[]
        {
            hero,
            new LayoutRect(others[0], heroW, 0,         sideW, rowH, m.Index),
            new LayoutRect(others[1], heroW, rowH,      sideW, rowH, m.Index),
            new LayoutRect(others[2], heroW, 2 * rowH,  sideW, m.WorkingAreaPhysical.Height - 2 * rowH, m.Index)
        };
    }
}

public sealed class FourRowTemplate : LayoutTemplate
{
    public override string Name => "4-Row";
    public override string Description => "Four equal horizontal strips.";
    public override string IconGlyph => "▬▬▬▬";
    public override bool Supports(int n) => n == 4;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var h = m.WorkingAreaPhysical.Height / 4;
        return new[]
        {
            new LayoutRect(0, 0, 0,         m.WorkingAreaPhysical.Width, h, m.Index),
            new LayoutRect(1, 0, h,        m.WorkingAreaPhysical.Width, h, m.Index),
            new LayoutRect(2, 0, 2 * h,    m.WorkingAreaPhysical.Width, h, m.Index),
            new LayoutRect(3, 0, 3 * h,    m.WorkingAreaPhysical.Width, m.WorkingAreaPhysical.Height - 3 * h, m.Index)
        };
    }
}

public sealed class FourColumnTemplate : LayoutTemplate
{
    public override string Name => "4-Column";
    public override string Description => "Four equal vertical columns.";
    public override string IconGlyph => "▮▮▮▮";
    public override bool Supports(int n) => n == 4;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var w = m.WorkingAreaPhysical.Width / 4;
        return new[]
        {
            new LayoutRect(0, 0, 0,         w, m.WorkingAreaPhysical.Height, m.Index),
            new LayoutRect(1, w, 0,         w, m.WorkingAreaPhysical.Height, m.Index),
            new LayoutRect(2, 2 * w, 0,     w, m.WorkingAreaPhysical.Height, m.Index),
            new LayoutRect(3, 3 * w, 0,     m.WorkingAreaPhysical.Width - 3 * w, m.WorkingAreaPhysical.Height, m.Index)
        };
    }
}

public sealed class FocusPlusThreeBottomTemplate : LayoutTemplate
{
    public readonly int HeroIndex;
    public FocusPlusThreeBottomTemplate(int heroIndex = 0) => HeroIndex = heroIndex;

    public override string Name => $"Focus + 3-Bottom (hero #{HeroIndex + 1})";
    public override string Description => "1 large hero on top + 3 small in the bottom row.";
    public override string IconGlyph => "▬\n▬▬▬";
    public override bool Supports(int n) => n == 4;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var heroH = (int)(m.WorkingAreaPhysical.Height * 0.6);
        var botH = m.WorkingAreaPhysical.Height - heroH;
        var w3 = m.WorkingAreaPhysical.Width / 3;
        var hero = new LayoutRect(HeroIndex, 0, 0, m.WorkingAreaPhysical.Width, heroH, m.Index);
        var others = Enumerable.Range(0, 4).Where(i => i != HeroIndex).ToArray();
        return new[]
        {
            hero,
            new LayoutRect(others[0], 0,        heroH, w3,              botH, m.Index),
            new LayoutRect(others[1], w3,       heroH, w3,              botH, m.Index),
            new LayoutRect(others[2], 2 * w3,   heroH, m.WorkingAreaPhysical.Width - 2 * w3, botH, m.Index)
        };
    }
}

using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Layout.Templates;

/// <summary>For N=5..8 windows, this provides "1 hero + N-1 in a grid" with a randomized hero slot.</summary>
public sealed class HeroPlusGridTemplate : LayoutTemplate
{
    public readonly int N;
    public readonly int HeroIndex;

    public HeroPlusGridTemplate(int n, int heroIndex)
    {
        N = n; HeroIndex = heroIndex;
    }

    public override string Name => $"Main + (N-1) Grid (hero #{HeroIndex + 1})";
    public override string Description => $"1 large hero + {N - 1} windows in a grid.";
    public override string IconGlyph => "▦";
    public override bool Supports(int count) => count == N;

    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        // Hero takes the left 55%, others stack in a grid on the right
        var heroW = (int)(m.WorkingAreaPhysical.Width * 0.55);
        var sideW = m.WorkingAreaPhysical.Width - heroW;
        var others = Enumerable.Range(0, N).Where(i => i != HeroIndex).ToArray();
        var rest = N - 1;
        var (cols, rows) = EqualGridTemplate.BestGrid(rest, sideW, m.WorkingAreaPhysical.Height);
        var cw = sideW / cols;
        var ch = m.WorkingAreaPhysical.Height / rows;
        var rects = new LayoutRect[N];
        rects[HeroIndex] = new LayoutRect(HeroIndex, 0, 0, heroW, m.WorkingAreaPhysical.Height, m.Index);
        for (int i = 0; i < rest; i++)
        {
            int r = i / cols, c = i % cols;
            int x = heroW + c * cw;
            int y = r * ch;
            int w = cw;
            int h = ch;
            // The last cell to render absorbs any leftover width (last col) and height (last row)
            bool lastInRow = (c == cols - 1) || (i == rest - 1);
            bool lastInCol = (i == rest - 1);
            if (lastInRow) w = sideW - c * cw;
            if (lastInCol) h = m.WorkingAreaPhysical.Height - r * ch;
            rects[others[i]] = new LayoutRect(others[i], x, y, w, h, m.Index);
        }
        return rects;
    }
}

public sealed class CascadeTemplate : LayoutTemplate
{
    public readonly int N;
    public CascadeTemplate(int n) => N = n;
    public override string Name => "Cascade";
    public override string Description => "Cascading sizes — biggest on left, shrinking right.";
    public override string IconGlyph => "▰▰▰";
    public override bool Supports(int count) => count == N;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var w = m.WorkingAreaPhysical.Width / N;
        var rects = new LayoutRect[N];
        for (int i = 0; i < N; i++)
        {
            int width = (i == N - 1) ? m.WorkingAreaPhysical.Width - i * w : w;
            rects[i] = new LayoutRect(i, i * w, 0, width, m.WorkingAreaPhysical.Height, m.Index);
        }
        return rects;
    }
}

public sealed class TopPlusRowTemplate : LayoutTemplate
{
    public readonly int N;
    public readonly int HeroIndex;
    public TopPlusRowTemplate(int n, int heroIndex) { N = n; HeroIndex = heroIndex; }
    public override string Name => $"Top Hero + Row (hero #{HeroIndex + 1})";
    public override string Description => "1 large hero on top + the rest in a row at the bottom.";
    public override string IconGlyph => "▬\n▬▬▬";
    public override bool Supports(int count) => count == N;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        var heroH = (int)(m.WorkingAreaPhysical.Height * 0.65);
        var botH = m.WorkingAreaPhysical.Height - heroH;
        var rest = N - 1;
        var w = m.WorkingAreaPhysical.Width / rest;
        var others = Enumerable.Range(0, N).Where(i => i != HeroIndex).ToArray();
        var rects = new LayoutRect[N];
        rects[HeroIndex] = new LayoutRect(HeroIndex, 0, 0, m.WorkingAreaPhysical.Width, heroH, m.Index);
        for (int i = 0; i < rest; i++)
        {
            int width = (i == rest - 1) ? m.WorkingAreaPhysical.Width - i * w : w;
            rects[others[i]] = new LayoutRect(others[i], i * w, heroH, width, botH, m.Index);
        }
        return rects;
    }
}

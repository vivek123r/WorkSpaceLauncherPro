using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Layout.Templates;

/// <summary>NxM equal cells. For N=4 → 2x2, N=6 → 2x3 or 3x2, etc.</summary>
public sealed class EqualGridTemplate : LayoutTemplate
{
    public override string Name => "Equal Grid";
    public override string Description => "All windows in equal-sized cells covering the full screen.";
    public override string IconGlyph => "▦";

    public override bool Supports(int n) => n is >= 2 and <= 16;

    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
    {
        // Pick rows/cols by aspect ratio
        var (cols, rows) = BestGrid(n, m.WorkingAreaPhysical.Width, m.WorkingAreaPhysical.Height);
        var cw = m.WorkingAreaPhysical.Width / cols;
        var ch = m.WorkingAreaPhysical.Height / rows;
        var rects = new LayoutRect[n];
        for (int i = 0; i < n; i++)
        {
            int r = i / cols;
            int c = i % cols;
            rects[i] = new LayoutRect(
                SlotIndex: i,
                X: c * cw,
                Y: r * ch,
                Width: (c == cols - 1) ? m.WorkingAreaPhysical.Width - c * cw : cw,
                Height: (r == rows - 1) ? m.WorkingAreaPhysical.Height - r * ch : ch,
                MonitorIndex: m.Index);
        }
        return rects;
    }

    public static (int Cols, int Rows) BestGrid(int n, int width, int height)
    {
        if (n <= 0) return (1, 1);
        // Find a (cols, rows) that exactly fits `n` cells with the best cell aspect ratio.
        int bestCols = 1, bestRows = n;
        double bestScore = double.MaxValue;
        for (int c = 1; c <= n; c++)
        {
            // Try both exact (n % c == 0) and "ceil" (n + c - 1) / c
            for (int exact = 1; exact >= 0; exact--)
            {
                int r = exact == 1 && n % c == 0 ? n / c : (n + c - 1) / c;
                if (r == 0) continue;
                var cw = (double)width / c;
                var ch = (double)height / r;
                var cellAspect = cw / ch;
                var target = 16.0 / 9.0;
                var unused = (c * r) - n;
                var aspectScore = Math.Abs(Math.Log(cellAspect / target));
                // Heavy penalty for unused cells; reward exact fit.
                var score = aspectScore + unused * 1.0;
                if (score < bestScore) { bestScore = score; bestCols = c; bestRows = r; }
            }
        }
        return (bestCols, bestRows);
    }
}

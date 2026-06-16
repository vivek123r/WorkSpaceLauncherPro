using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;
using WorkSpaceLauncherPro.Layout.Templates;

namespace WorkSpaceLauncherPro.Layout;

/// <summary>
/// Maps (windowCount) → list of applicable LayoutTemplate instances. For each N, returns
/// at least 6 distinct templates so the Smart Layout Picker has a useful gallery.
/// </summary>
public sealed class LayoutTemplateRegistry
{
    public IReadOnlyList<LayoutTemplate> TemplatesFor(int windowCount)
    {
        if (windowCount <= 0) return Array.Empty<LayoutTemplate>();
        if (windowCount == 1) return new LayoutTemplate[] { new SingleFullscreenTemplate() };

        var baseTemplates = windowCount switch
        {
            2 => new LayoutTemplate[]
            {
                new HalvesTemplate(),
                new VerticalStackTemplate(),
                new MainSideTemplate(0, 0.7),
                new MainSideTemplate(1, 0.7),
                new MainSideTemplate(0, 0.8),
                new MainSideTemplate(1, 0.8),
            },
            3 => new LayoutTemplate[]
            {
                new TripleColumnTemplate(),
                new ThirdsTemplate(),
                new MainPlusTwoStackTemplate(0),
                new MainPlusTwoStackTemplate(1),
                new MainPlusTwoStackTemplate(2),
                new FocusPlusTwoBottomTemplate(0),
                new FocusPlusTwoBottomTemplate(1),
                new FocusPlusTwoBottomTemplate(2),
            },
            4 => new LayoutTemplate[]
            {
                new EqualGridTemplate(),
                new MainPlusThreeStackTemplate(0),
                new MainPlusThreeStackTemplate(1),
                new MainPlusThreeStackTemplate(2),
                new MainPlusThreeStackTemplate(3),
                new FourRowTemplate(),
                new FourColumnTemplate(),
                new FocusPlusThreeBottomTemplate(0),
                new FocusPlusThreeBottomTemplate(1),
                new FocusPlusThreeBottomTemplate(2),
                new FocusPlusThreeBottomTemplate(3),
            },
            >= 5 and <= 8 => BuildFiveToEight(windowCount),
            _ => BuildMany(windowCount)
        };

        // Always pad with a "randomize hero" variant for asymmetric templates
        var padded = new List<LayoutTemplate>(baseTemplates);
        if (windowCount >= 2 && windowCount <= 8)
        {
            // Ensure at least 6 by adding HeroPlusGridTemplate variants
            for (int h = 0; h < windowCount && padded.Count < 12; h++)
            {
                if (!padded.OfType<HeroPlusGridTemplate>().Any(x => x.HeroIndex == h && x.N == windowCount))
                    padded.Add(new HeroPlusGridTemplate(windowCount, h));
            }
        }
        return padded;
    }

    private static LayoutTemplate[] BuildFiveToEight(int n) => new LayoutTemplate[]
    {
        new EqualGridTemplate(),
        new HeroPlusGridTemplate(n, 0),
        new HeroPlusGridTemplate(n, 1),
        new HeroPlusGridTemplate(n, 2),
        new HeroPlusGridTemplate(n, n - 1),
        new CascadeTemplate(n),
        new TopPlusRowTemplate(n, 0),
        new TopPlusRowTemplate(n, 1),
    };

    private static LayoutTemplate[] BuildMany(int n) => new LayoutTemplate[]
    {
        new EqualGridTemplate(),
        new HeroPlusGridTemplate(n, 0)
    };
}

public sealed class SingleFullscreenTemplate : LayoutTemplate
{
    public override string Name => "Fullscreen";
    public override string Description => "Single window fills the monitor.";
    public override string IconGlyph => "▰";
    public override bool Supports(int n) => n == 1;
    public override IReadOnlyList<LayoutRect> Generate(int n, MonitorInfo m)
        => new[] { new LayoutRect(0, 0, 0, m.WorkingAreaPhysical.Width, m.WorkingAreaPhysical.Height, m.Index) };
}

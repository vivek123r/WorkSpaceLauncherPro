using Microsoft.Extensions.Logging;
using WorkSpaceLauncherPro.Core.Models;
using WorkSpaceLauncherPro.Core.Windowing;
using WorkSpaceLauncherPro.Layout.Templates;

namespace WorkSpaceLauncherPro.Layout;

public sealed class LayoutEngine
{
    private readonly LayoutTemplateRegistry _registry;
    private readonly ILogger<LayoutEngine> _log;

    public LayoutEngine(LayoutTemplateRegistry registry, ILogger<LayoutEngine> log)
    {
        _registry = registry;
        _log = log;
    }

    /// <summary>For the given window count, return ≥6 distinct templates.</summary>
    public IReadOnlyList<LayoutTemplate> SuggestLayouts(int windowCount, MonitorInfo target)
    {
        var all = _registry.TemplatesFor(windowCount);
        _log.LogInformation("Suggested {Count} layouts for {N} windows on {Device}",
            all.Count, windowCount, target.DeviceName);
        return all;
    }

    /// <summary>Default layout = "Equal Grid" for small N, "Hero+Grid" for large N.</summary>
    public LayoutTemplate DefaultFor(int n, MonitorInfo target)
    {
        LayoutTemplate template = n switch
        {
            <= 1 => new SingleFullscreenTemplate(),
            2    => new HalvesTemplate(),
            3    => new TripleColumnTemplate(),
            4    => new EqualGridTemplate(),
            <= 8 => new HeroPlusGridTemplate(n, 0),
            _    => new EqualGridTemplate()
        };
        return template;
    }

    /// <summary>Generate a fresh "randomized hero" variant of an asymmetric template.</summary>
    public LayoutTemplate WithShuffledHero(LayoutTemplate original, int windowCount)
    {
        return original switch
        {
            HeroPlusGridTemplate hpg => new HeroPlusGridTemplate(windowCount, Random.Shared.Next(windowCount)),
            MainPlusTwoStackTemplate => new MainPlusTwoStackTemplate(Random.Shared.Next(3)),
            MainPlusThreeStackTemplate => new MainPlusThreeStackTemplate(Random.Shared.Next(4)),
            FocusPlusTwoBottomTemplate => new FocusPlusTwoBottomTemplate(Random.Shared.Next(3)),
            FocusPlusThreeBottomTemplate => new FocusPlusThreeBottomTemplate(Random.Shared.Next(4)),
            TopPlusRowTemplate tpr => new TopPlusRowTemplate(windowCount, Random.Shared.Next(windowCount)),
            _ => original
        };
    }
}

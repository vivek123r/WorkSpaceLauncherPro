using WorkSpaceLauncherPro.Core.Windowing;

namespace WorkSpaceLauncherPro.Core.Models;

/// <summary>Base class for all layout templates — pure functions from (count, monitor) to rects.</summary>
public abstract class LayoutTemplate
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual string IconGlyph => "▦";

    /// <summary>True if this template supports the given window count.</summary>
    public abstract bool Supports(int windowCount);

    /// <summary>Compute exactly <paramref name="windowCount"/> non-overlapping rects that cover
    /// the target monitor's working area. Rects are in **monitor-relative** physical pixels.</summary>
    public abstract IReadOnlyList<LayoutRect> Generate(int windowCount, MonitorInfo target);
}

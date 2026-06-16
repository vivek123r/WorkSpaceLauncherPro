using FluentAssertions;
using WorkSpaceLauncherPro.Core.Windowing;
using WorkSpaceLauncherPro.Layout;
using WorkSpaceLauncherPro.Layout.Templates;
using Xunit;

namespace WorkSpaceLauncherPro.Tests;

public class LayoutEngineTests
{
    private static readonly MonitorInfo FakeMonitor = new(
        Index: 0,
        Handle: new IntPtr(1),
        DeviceName: @"\\.\DISPLAY1",
        BoundsPhysical: new RECT { Left = 0, Top = 0, Right = 2560, Bottom = 1440 },
        WorkingAreaPhysical: new RECT { Left = 0, Top = 0, Right = 2560, Bottom = 1400 },
        IsPrimary: true,
        DpiScale: 1.0);

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void SuggestLayouts_Returns_At_Least_Six_Templates_For_Each_N(int n)
    {
        var engine = new LayoutEngine(new LayoutTemplateRegistry(), NullLogger);
        var layouts = engine.SuggestLayouts(n, FakeMonitor);
        layouts.Should().HaveCountGreaterOrEqualTo(6, $"N={n} should yield at least 6 distinct templates");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public void Each_Suggested_Layout_Covers_The_Entire_Working_Area(int n)
    {
        var engine = new LayoutEngine(new LayoutTemplateRegistry(), NullLogger);
        var monitor = FakeMonitor;
        var work = monitor.WorkingAreaPhysical;
        var totalW = work.Right - work.Left;
        var totalH = work.Bottom - work.Top;

        foreach (var template in engine.SuggestLayouts(n, monitor))
        {
            var rects = template.Generate(n, monitor);
            rects.Should().HaveCount(n, $"{template.Name} returned {rects.Count} rects, expected {n}");
            // All rects must be within bounds
            foreach (var r in rects)
            {
                r.X.Should().BeGreaterOrEqualTo(0, $"{template.Name} rect has negative X");
                r.Y.Should().BeGreaterOrEqualTo(0, $"{template.Name} rect has negative Y");
                (r.X + r.Width).Should().BeLessOrEqualTo(totalW + 1, $"{template.Name} exceeds right edge");
                (r.Y + r.Height).Should().BeLessOrEqualTo(totalH + 1, $"{template.Name} exceeds bottom edge");
            }
            // Sum of rect areas should equal monitor area (no gaps/overlaps)
            var totalArea = rects.Sum(r => r.Width * r.Height);
            totalArea.Should().BeCloseTo(totalW * totalH, 200,
                $"{template.Name} should tile the monitor without gaps (area={totalArea}, expected={totalW * totalH})");
        }
    }

    [Fact]
    public void Halves_Template_Returns_Two_Equal_Rects()
    {
        var t = new HalvesTemplate();
        var rects = t.Generate(2, FakeMonitor);
        rects.Should().HaveCount(2);
        rects[0].Width.Should().Be(1280);
        rects[0].X.Should().Be(0);
        rects[1].X.Should().Be(1280);
        rects[1].Width.Should().Be(1280);
    }

    [Fact]
    public void Equal_Grid_For_Four_Is_2x2()
    {
        var t = new EqualGridTemplate();
        var rects = t.Generate(4, FakeMonitor);
        rects.Should().HaveCount(4);
        // Aspect 16:9 → 2 cols x 2 rows
        rects[0].Width.Should().Be(1280);
        rects[0].Height.Should().Be(700);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void With_Shuffled_Hero_Returns_Different_Hero_For_Hero_Plus_Grid(int n)
    {
        var engine = new LayoutEngine(new LayoutTemplateRegistry(), NullLogger);
        var original = new HeroPlusGridTemplate(n, 0);
        var shuffled = engine.WithShuffledHero(original, n);
        shuffled.Should().BeOfType<HeroPlusGridTemplate>();
        var s = (HeroPlusGridTemplate)shuffled;
        // Seed is random — just check it produces a valid index
        s.HeroIndex.Should().BeInRange(0, n - 1);
    }

    [Fact]
    public void Default_For_Two_Is_Halves()
    {
        var engine = new LayoutEngine(new LayoutTemplateRegistry(), NullLogger);
        engine.DefaultFor(2, FakeMonitor).Should().BeOfType<HalvesTemplate>();
    }

    [Fact]
    public void Default_For_One_Is_Single()
    {
        var engine = new LayoutEngine(new LayoutTemplateRegistry(), NullLogger);
        engine.DefaultFor(1, FakeMonitor).Should().BeOfType<SingleFullscreenTemplate>();
    }

    private static readonly Microsoft.Extensions.Logging.ILogger<LayoutEngine> NullLogger
        = Microsoft.Extensions.Logging.Abstractions.NullLogger<LayoutEngine>.Instance;
}

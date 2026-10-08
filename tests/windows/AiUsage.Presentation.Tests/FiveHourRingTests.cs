using AiUsage.Features.Ledger;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>AIU-055 R-04 / AC-03: the five-hour ring draws the arc from 12 o'clock clockwise; the control only places it.</summary>
public sealed class FiveHourRingTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void ArcEndsFollowTheClock()
    {
        AssertEnd(0, 8, 1);
        AssertEnd(0.25, 15, 8);
        AssertEnd(0.5, 8, 15);
        AssertEnd(0.75, 1, 8);

        Assert.False(RingGeometry.IsLargeArc(0.5));
        Assert.True(RingGeometry.IsLargeArc(0.51));

        Assert.Equal(1, RingGeometry.Clamp(1.4));
        Assert.Equal(0, RingGeometry.Clamp(-0.2));
        Assert.Equal(0, RingGeometry.Clamp(double.NaN));
    }

    [Fact]
    public void GeometryMatchesTheSixteenPixelRing()
    {
        Assert.Equal(16, RingGeometry.Size);
        Assert.Equal(2, RingGeometry.Stroke);
        Assert.Equal(7, RingGeometry.Radius);
    }

    [Fact]
    public void ValuesOutsideTheRangeDrawLikeTheirClampedValue()
    {
        AssertEnd(1.4, 8, 1);
        AssertEnd(-0.2, 8, 1);
        AssertEnd(double.NaN, 8, 1);
        Assert.False(RingGeometry.IsLargeArc(double.NaN));
        Assert.True(RingGeometry.IsLargeArc(1.4));
    }

    private static void AssertEnd(double fraction, double x, double y)
    {
        var (actualX, actualY) = RingGeometry.ArcEnd(fraction);
        Assert.InRange(actualX, x - Tolerance, x + Tolerance);
        Assert.InRange(actualY, y - Tolerance, y + Tolerance);
    }
}

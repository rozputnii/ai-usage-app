using AiUsage.Features.Ledger;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>
/// The usage bars fill as one strip from the left (owner picks 2026-10-11): the parts' right edges move one after another,
/// left to right, at one speed, 450 ms for a full bar, slowing only at the end of the last move.
/// </summary>
public sealed class BarSweepTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void FromEmptyThePartsGrowOneAfterAnotherAtOneSpeed()
    {
        var sweep = new BarSweep([0, 0, 0], [0.25, 0.75, 1]);

        Assert.Equal(450, sweep.Duration, Tolerance);
        // The linear part runs at 1/360 of the bar per millisecond; the last move takes twice its linear time and eases out.
        AssertEdges([0.125, 0.125, 0.125], sweep.At(45));
        AssertEdges([0.25, 0.25, 0.25], sweep.At(90));
        AssertEdges([0.25, 0.5, 0.5], sweep.At(180));
        AssertEdges([0.25, 0.75, 0.75], sweep.At(270));
        AssertEdges([0.25, 0.75, 0.9375], sweep.At(360));
        AssertEdges([0.25, 0.75, 1], sweep.At(450));
        AssertEdges([0, 0, 0], sweep.At(0));
        AssertEdges([0.25, 0.75, 1], sweep.At(10_000));
    }

    [Fact]
    public void EachPartGrowsFromItsLeftEdgeAndNoEdgeJumps()
    {
        var sweep = new BarSweep([0, 0, 0, 0], [0.1, 0.45, 0.5, 1]);
        var previous = sweep.At(0);
        var speeds = new List<double>();
        for (var t = 1.0; t <= sweep.Duration; t++)
        {
            var edges = sweep.At(t);
            for (var k = 0; k < edges.Length; k++)
            {
                // Edges stay in order, move only forward here, and move a small step per millisecond.
                Assert.True(k == 0 || edges[k] >= edges[k - 1] - Tolerance);
                Assert.True(edges[k] >= previous[k] - Tolerance);
                Assert.True(edges[k] - previous[k] <= 2 / 450.0 + Tolerance);
            }
            // At most one edge leads at a time; every edge before it is already in place, so a growing part's left edge stays put.
            var leading = Enumerable.Range(0, edges.Length).Where(k => Math.Abs(edges[k] - previous[k]) > Tolerance && (k == 0 || Math.Abs(edges[k] - edges[k - 1]) > Tolerance)).ToArray();
            Assert.True(leading.Length <= 1);
            if (leading is [var lead])
            {
                for (var k = 0; k < lead; k++)
                    Assert.Equal(sweep.At(sweep.Duration)[k], edges[k], Tolerance);
                if (lead < edges.Length - 1)
                    speeds.Add(edges[lead] - previous[lead]);
            }
            previous = edges;
        }
        // One constant speed until the last part: the bar moves 1 + 0.5 linear units (the last move counts twice) in 450 ms.
        Assert.NotEmpty(speeds);
        Assert.All(speeds.Skip(1).SkipLast(1), speed => Assert.Equal(1.5 / 450, speed, 1e-6));
    }

    [Fact]
    public void OnlyTheEndOfTheLastMoveEasesOut()
    {
        var sweep = new BarSweep([0, 0], [0.5, 1]);
        // 0.5 + 2 × 0.5 linear units in 450 ms: 300 ms per bar, so the first part takes 150 ms at a constant speed.
        Assert.Equal(0.25, sweep.At(75)[0], Tolerance);
        // The last move starts at the same speed and slows to a stop.
        var start = sweep.At(151)[1] - sweep.At(150)[1];
        var end = sweep.At(450)[1] - sweep.At(449)[1];
        Assert.Equal(1 / 300.0, start, 1e-5);
        Assert.True(end < start / 50);
    }

    [Fact]
    public void AValueChangeMovesOnlyTheEdgesThatChange()
    {
        // Used grows from 30 % to 35 %: the allowed part's left edge moves, its right edge and the grey part stay.
        var sweep = new BarSweep([0.3, 0.7, 1], [0.35, 0.7, 1]);
        Assert.Equal(0.05 * 450, sweep.Duration, Tolerance);
        AssertEdges([0.3375, 0.7, 1], sweep.At(sweep.Duration / 2));
        AssertEdges([0.35, 0.7, 1], sweep.At(sweep.Duration));
    }

    [Fact]
    public void ADecreaseMovesLeftToRightAsWell()
    {
        var sweep = new BarSweep([0.5, 0.8, 1], [0.2, 0.5, 1]);
        Assert.Equal(0.6 * 450, sweep.Duration, Tolerance);
        var half = sweep.Duration * 0.3 / 0.9 / 2;
        // The first edge moves first; the second waits in its old place.
        AssertEdges([0.35, 0.8, 1], sweep.At(half));
        AssertEdges([0.2, 0.8, 1], sweep.At(half * 2));
        AssertEdges([0.2, 0.5, 1], sweep.At(sweep.Duration));
    }

    [Fact]
    public void AnEdgeThatPassesTheNextOnePushesIt()
    {
        // The used part grows past the old end of the allowed part, which is pushed ahead and then moves on from there.
        var sweep = new BarSweep([0.3, 0.4, 1], [0.8, 0.9, 1]);
        Assert.Equal((0.5 + 0.1) * 450, sweep.Duration, Tolerance);
        var mid = sweep.At(sweep.Duration * 0.6 / 0.7 * 0.5 / 0.6 * 0.5);
        Assert.Equal(0.55, mid[0], Tolerance);
        Assert.Equal(0.55, mid[1], Tolerance);
        AssertEdges([0.8, 0.9, 1], sweep.At(sweep.Duration));
    }

    [Fact]
    public void ANewPartStartsEmptyAtTheOldEndOfTheBar()
    {
        var sweep = new BarSweep([0.5, 1], [0.4, 0.6, 1]);
        AssertEdges([0.5, 1, 1], sweep.At(0));
        // The new last part opens as the edge before it moves back; the bar's end never moves.
        Assert.All(Enumerable.Range(0, 200), t => Assert.Equal(1, sweep.At(t)[2], Tolerance));
        AssertEdges([0.4, 0.6, 1], sweep.At(sweep.Duration));
        var still = new BarSweep([0.4, 0.6, 1], [0.4, 0.6, 1]);
        Assert.Equal(0, still.Duration);
        AssertEdges([0.4, 0.6, 1], still.At(0));
    }

    [Fact]
    public void StripEdgesFollowTheCellAndPartWeightsWithGapsBetweenCells()
    {
        // 208 px: two cells of weights 1 and 3 with a 8 px gap leave 200 px, so 50 and 150 px. Each cell's parts end in a
        // filler up to the next cell (the gap) or to the end of the bar.
        var edges = BarSweep.StripEdges([1, 3], [[1, 1], [2, 1]], 208, 8);
        AssertEdges([25 / 208.0, 50 / 208.0, 58 / 208.0, 158 / 208.0, 1, 1], edges);
        // Parts that sum to zero leave the cell empty.
        AssertEdges([0, 0, 1], BarSweep.StripEdges([1], [[0, 0]], 100, 4));
        Assert.Empty(BarSweep.StripEdges([], [], 100, 4));
    }

    [Fact]
    public void PeriodBarEdgesFillTheTrackBetweenSegments()
    {
        // Left mode mirrors the bar: the track shows before and after the painted parts.
        var (edges, parts) = BarSweep.BarEdges([(30, 20), (50, 10)]);
        AssertEdges([0.3, 0.5, 0.6, 1], edges);
        Assert.Equal([1, 2], parts);
        var (full, fullParts) = BarSweep.BarEdges([(0, 40), (40, 60)]);
        AssertEdges([0.4, 1], full);
        Assert.Equal([0, 1], fullParts);
        var (none, noParts) = BarSweep.BarEdges([]);
        AssertEdges([1], none);
        Assert.Empty(noParts);
    }

    private static void AssertEdges(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], actual[i], Tolerance);
    }
}

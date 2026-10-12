namespace AiUsage.Features.Ledger;

/// <summary>
/// How a usage bar fills (owner picks 2026-10-11). A bar is a row of consecutive parts from 0 to 1 of its width, painted
/// parts and the track or gaps between them, given by the parts' right edges. The edges move from where they were shown
/// to their new places one after another, left to right, at one speed, so the bar reads as one strip and every part
/// grows from its left edge. An edge that another one passes is pushed ahead of it. A full bar takes 450 ms; the last
/// move takes twice its linear time and slows to a stop, so it starts at the same speed.
/// </summary>
internal sealed class BarSweep
{
    public const double FullBarMilliseconds = 450;
    private const double Epsilon = 1e-9;
    private readonly double[] from;
    private readonly double[] start;
    private readonly double[] to;
    private readonly double[] begin;
    private readonly double[] length;
    private readonly int last = -1;

    /// <summary>Edges shown now (empty is all zero) to their new places; a part that did not exist starts empty at the old end.</summary>
    public BarSweep(IReadOnlyList<double> from, IReadOnlyList<double> to)
    {
        var n = to.Count;
        var end = from.Count > 0 ? from[^1] : 0;
        this.from = [.. Enumerable.Range(0, n).Select(k => k < from.Count ? from[k] : end)];
        this.to = [.. to];
        start = new double[n];
        length = new double[n];
        begin = new double[n];
        for (var k = 0; k < n; k++)
        {
            start[k] = Math.Max(this.from[k], k == 0 ? 0 : this.to[k - 1]);
            if (Math.Abs(this.to[k] - start[k]) > Epsilon)
                last = k;
        }
        var travel = Enumerable.Range(0, n).Sum(k => Math.Abs(this.to[k] - start[k]));
        if (last < 0)
            return;
        Duration = travel * FullBarMilliseconds;
        var perUnit = Duration / (travel + Math.Abs(this.to[last] - start[last]));
        var at = 0.0;
        for (var k = 0; k < n; k++)
        {
            begin[k] = at;
            length[k] = Math.Abs(this.to[k] - start[k]) * perUnit * (k == last ? 2 : 1);
            at += length[k];
        }
    }

    public double Duration { get; }

    /// <summary>The edges after this many milliseconds.</summary>
    public double[] At(double milliseconds)
    {
        var edges = new double[to.Length];
        var previous = 0.0;
        for (var k = 0; k < edges.Length; k++)
        {
            if (k > last || milliseconds >= begin[k] + length[k])
                edges[k] = to[k];
            else if (milliseconds <= begin[k])
                edges[k] = Math.Max(from[k], previous);
            else
            {
                var p = (milliseconds - begin[k]) / length[k];
                edges[k] = start[k] + (to[k] - start[k]) * (k == last ? 1 - (1 - p) * (1 - p) : p);
            }
            previous = edges[k];
        }
        return edges;
    }

    /// <summary>
    /// The today strip's edges: per cell its parts, then a filler up to the next cell (the gap) or to the bar's end. Cells
    /// share the width less the gaps by weight, and parts share their cell by weight, as the strip's star columns do.
    /// </summary>
    public static double[] StripEdges(IReadOnlyList<double> cellWeights, IReadOnlyList<IReadOnlyList<double>> partWeights, double width, double gap)
    {
        var edges = new List<double>();
        if (cellWeights.Count == 0 || width <= 0)
            return [];
        var room = Math.Max(0, width - gap * (cellWeights.Count - 1));
        var cells = cellWeights.Sum();
        var left = 0.0;
        for (var i = 0; i < cellWeights.Count; i++)
        {
            var cell = cells > 0 ? room * cellWeights[i] / cells : 0;
            var parts = partWeights[i].Sum();
            var x = left;
            foreach (var part in partWeights[i])
            {
                x += parts > 0 ? cell * part / parts : 0;
                edges.Add(x / width);
            }
            left += cell + (i < cellWeights.Count - 1 ? gap : 0);
            edges.Add(i < cellWeights.Count - 1 ? left / width : 1);
        }
        return [.. edges];
    }

    /// <summary>A period bar's edges from its segments in percent, with the track between them; Parts maps each segment to its part.</summary>
    public static (double[] Edges, int[] Parts) BarEdges(IReadOnlyList<(double Left, double Width)> segments)
    {
        var edges = new List<double>();
        var parts = new int[segments.Count];
        var cursor = 0.0;
        foreach (var i in Enumerable.Range(0, segments.Count).OrderBy(i => segments[i].Left))
        {
            var left = Math.Clamp(segments[i].Left / 100, cursor, 1);
            if (left > cursor + Epsilon)
                edges.Add(left);
            cursor = Math.Clamp(left + segments[i].Width / 100, left, 1);
            parts[i] = edges.Count;
            edges.Add(cursor);
        }
        if (edges.Count == 0 || cursor < 1 - Epsilon)
            edges.Add(1);
        return ([.. edges], parts);
    }
}

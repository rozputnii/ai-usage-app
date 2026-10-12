namespace AiUsage.Features.Ledger;

/// <summary>Where a card dragged by its grip lands among the other account cards (T-055 R-10).</summary>
internal static class ReorderMath
{
    /// <summary>The dragged card's place among the others: how many of their midpoints lie above the pointer.</summary>
    public static int InsertionIndex(IReadOnlyList<double> otherMidpoints, double pointerY) => otherMidpoints.Count(midpoint => midpoint < pointerY);

    /// <summary>The account the moved one goes before at that place among the others, or null for last.</summary>
    public static string? BeforeId(IReadOnlyList<string> order, string moved, int insertionIndex)
    {
        var others = order.Where(id => id != moved).ToList();
        return insertionIndex < others.Count ? others[insertionIndex] : null;
    }

    /// <summary>
    /// The grid's children after the dragged card lands at that place among the other cards, as indexes into the children
    /// now. A child that is not a card (the history panel) moves with the card before it.
    /// </summary>
    public static int[] LandingOrder(IReadOnlyList<bool> isCard, int dragged, int insertionIndex)
    {
        var blocks = new List<List<int>>();
        for (var i = 0; i < isCard.Count; i++)
            if (isCard[i] || blocks.Count == 0)
                blocks.Add([i]);
            else
                blocks[^1].Add(i);
        var moved = blocks.Single(block => block[0] == dragged);
        blocks.Remove(moved);
        blocks.Insert(Math.Clamp(insertionIndex, 0, blocks.Count), moved);
        return [.. blocks.SelectMany(block => block)];
    }

    /// <summary>How far each child slides from its place now to its place in that order, stacked with the grid's spacing (variant C's live gap).</summary>
    public static double[] Offsets(IReadOnlyList<double> heights, double spacing, IReadOnlyList<int> order)
    {
        var offsets = new double[heights.Count];
        var now = 0.0;
        for (var i = 0; i < heights.Count; i++)
        {
            offsets[i] -= now;
            now += heights[i] + spacing;
        }
        var then = 0.0;
        foreach (var i in order)
        {
            offsets[i] += then;
            then += heights[i] + spacing;
        }
        return offsets;
    }
}

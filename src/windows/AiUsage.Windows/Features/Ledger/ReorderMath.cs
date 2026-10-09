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
}

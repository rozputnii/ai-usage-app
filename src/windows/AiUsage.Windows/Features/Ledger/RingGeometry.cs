namespace AiUsage.Features.Ledger;

/// <summary>
/// The five-hour ring's arc maths (AIU-055 R-04): a 16 px box, centre (8, 8), radius 7, y down. The arc starts at
/// 12 o'clock and runs clockwise, so a quarter ends at 3 o'clock.
/// </summary>
internal static class RingGeometry
{
    public const double Size = 16, Stroke = 2, Radius = 7;

    private const double Centre = Size / 2;

    /// <summary>The share to draw: 0 to 1, with NaN as 0.</summary>
    public static double Clamp(double fraction) => double.IsNaN(fraction) ? 0 : Math.Clamp(fraction, 0, 1);

    /// <summary>Where the arc ends after the given share of the circle.</summary>
    public static (double X, double Y) ArcEnd(double fraction)
    {
        var angle = Clamp(fraction) * 2 * Math.PI;
        return (Centre + Radius * Math.Sin(angle), Centre - Radius * Math.Cos(angle));
    }

    /// <summary>Whether the arc is longer than a half circle, which an arc segment needs to pick the right of its two circles.</summary>
    public static bool IsLargeArc(double fraction) => Clamp(fraction) > 0.5;
}

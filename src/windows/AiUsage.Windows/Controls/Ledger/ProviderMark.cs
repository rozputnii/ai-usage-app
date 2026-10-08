using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace AiUsage.Controls.Ledger;

/// <summary>A provider's monochrome mark (R-07): the 24 × 24 path of <see cref="ProviderMarkData"/> scaled to <c>size</c>.</summary>
internal static class ProviderMark
{
    public static FrameworkElement Create(ProviderKind provider, Brush brush, double size = 16) => new Viewbox
    {
        Width = size,
        Height = size,
        Child = new Path
        {
            Width = 24,
            Height = 24,
            Fill = brush,
            Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), ProviderMarkData.PathData(provider)),
        },
    };
}

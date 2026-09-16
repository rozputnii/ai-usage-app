using Microsoft.UI.Xaml.Data;

namespace AiUsage.Controls;

/// <summary>Lets x:Bind two-way selection bind typed view-model items to object-typed selector properties.</summary>
internal sealed partial class PassThroughConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language) => value;
    public object? ConvertBack(object? value, Type targetType, object? parameter, string language) => value;
}

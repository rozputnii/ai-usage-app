using Microsoft.Windows.ApplicationModel.Resources;

namespace AiUsage.Controls;

/// <summary>Resource lookup for view code-behind and x:Bind functions (view models use ITextResources).</summary>
internal static class Res
{
    private static readonly ResourceLoader Loader = new();

    public static string T(string key)
    {
        var value = Loader.GetString(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }

    public static string F(string key, params object?[] args) => string.Format(System.Globalization.CultureInfo.CurrentCulture, T(key), args);
}

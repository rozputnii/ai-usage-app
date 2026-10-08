using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Features.Ledger;

/// <summary>
/// Simplified monochrome provider marks (R-07), drawn by hand for this app as XAML path data in a 24 × 24 box and filled
/// in one colour. No stroke or gap is thinner than 1.5 units, so each mark still reads at 16 px. Claude's "F1" uses the
/// nonzero rule so its rays merge into the hub; the others use the default even-odd rule, so inner shapes cut holes.
/// </summary>
internal static class ProviderMarkData
{
    /// <summary>The spark: a round hub and twelve blunt rays of slightly uneven length and spacing.</summary>
    private const string Claude =
        "F1 M12,9.3 A2.7,2.7 0 0 1 12,14.7 A2.7,2.7 0 0 1 12,9.3 Z " +
        "M11.34,10.32 L11.74,1.23 A1.7,1.7 0 0 1 14.13,1.44 L12.94,10.46 Z " +
        "M12.3,10.22 L17.14,2.88 A1.7,1.7 0 0 1 19.09,4.29 L13.6,11.16 Z " +
        "M13.25,10.69 L21.08,7.42 A1.7,1.7 0 0 1 21.91,9.67 L13.8,12.2 Z " +
        "M13.73,11.49 L21.96,12.54 A1.7,1.7 0 0 1 21.55,14.9 L13.46,13.07 Z " +
        "M13.76,12.43 L20.21,17.32 A1.7,1.7 0 0 1 18.66,19.16 L12.73,13.65 Z " +
        "M13.35,13.2 L16.79,20.64 A1.7,1.7 0 0 1 14.56,21.54 L11.87,13.8 Z " +
        "M12.6,13.71 L11.96,22.18 A1.7,1.7 0 0 1 9.58,21.88 L11.01,13.51 Z " +
        "M11.66,13.78 L6.64,21.11 A1.7,1.7 0 0 1 4.72,19.67 L10.39,12.81 Z " +
        "M10.87,13.41 L2.35,17.82 A1.7,1.7 0 0 1 1.34,15.65 L10.19,11.96 Z " +
        "M10.33,12.69 L0.64,12.41 A1.7,1.7 0 0 1 0.81,10.01 L10.44,11.09 Z " +
        "M10.21,11.76 L2.29,6.9 A1.7,1.7 0 0 1 3.63,4.91 L11.1,10.43 Z " +
        "M10.57,10.89 L6.32,3.08 A1.7,1.7 0 0 1 8.47,2.03 L12.01,10.19 Z";

    /// <summary>
    /// The OpenAI knot: a six-lobed rim, a hexagon hole, and six slanted holes between bands that continue the
    /// hexagon's edges to the rim.
    /// </summary>
    private const string Codex =
        "M8.44,2.44 A6.08,6.08 0 0 1 18.5,4.14 A6.08,6.08 0 0 1 22.06,13.69 A6.08,6.08 0 0 1 15.56,21.56 " +
        "A6.08,6.08 0 0 1 5.5,19.86 A6.08,6.08 0 0 1 1.94,10.31 A6.08,6.08 0 0 1 8.44,2.44 Z " +
        "M12,9.29 L14.35,10.64 L14.35,13.36 L12,14.71 L9.65,13.36 L9.65,10.64 Z " +
        "M15.2,8.94 L20.79,12.17 A4.18,4.18 0 0 0 17.8,5.94 L15.2,7.44 Z " +
        "M16.25,13.24 L16.25,19.7 A4.18,4.18 0 0 0 20.15,13.99 L17.55,12.49 Z " +
        "M13.05,16.3 L7.46,19.53 A4.18,4.18 0 0 0 14.35,20.06 L14.35,17.05 Z " +
        "M8.8,15.06 L3.21,11.83 A4.18,4.18 0 0 0 6.2,18.06 L8.8,16.56 Z " +
        "M7.75,10.76 L7.75,4.3 A4.18,4.18 0 0 0 3.85,10.01 L6.45,11.51 Z " +
        "M10.95,7.7 L16.54,4.47 A4.18,4.18 0 0 0 9.65,3.94 L9.65,6.95 Z";

    /// <summary>The pilot: a domed helmet with side ears, two goggle holes, and a face hole holding two eye pills.</summary>
    private const string Copilot =
        "M12,2.2 C16.5,2.2 20.6,4.2 20.8,8.5 V12.4 C22.8,12.4 23.4,13.2 23.4,14.6 V17 C23.4,18.6 22.4,19.2 21,19.6 " +
        "C19.2,21 17,21.8 15.8,21.8 H8.2 C7,21.8 4.8,21 3,19.6 C1.6,19.2 0.6,18.6 0.6,17 V14.6 " +
        "C0.6,13.2 1.2,12.4 3.2,12.4 V8.5 C3.4,4.2 7.5,2.2 12,2.2 Z " +
        "M7.3,5.2 H8.7 A2.4,2.4 0 0 1 11.1,7.6 V8.8 A2.4,2.4 0 0 1 8.7,11.2 H7.3 A2.4,2.4 0 0 1 4.9,8.8 V7.6 " +
        "A2.4,2.4 0 0 1 7.3,5.2 Z " +
        "M15.3,5.2 H16.7 A2.4,2.4 0 0 1 19.1,7.6 V8.8 A2.4,2.4 0 0 1 16.7,11.2 H15.3 A2.4,2.4 0 0 1 12.9,8.8 V7.6 " +
        "A2.4,2.4 0 0 1 15.3,5.2 Z " +
        "M4.8,14.4 C4.8,13.4 5.6,12.8 6.8,12.8 H10 C11,12.8 11.2,12.3 12,12.3 C12.8,12.3 13,12.8 14,12.8 H17.2 " +
        "C18.4,12.8 19.2,13.4 19.2,14.4 V17.4 C19.2,19 17.8,20.2 16,20.2 H8 C6.2,20.2 4.8,19 4.8,17.4 Z " +
        "M8.4,15.3 A1,1 0 0 1 10.4,15.3 V17.3 A1,1 0 0 1 8.4,17.3 Z " +
        "M13.6,15.3 A1,1 0 0 1 15.6,15.3 V17.3 A1,1 0 0 1 13.6,17.3 Z";

    /// <summary>The arch: a bell-shaped "A" without a crossbar, flaring to two rounded feet.</summary>
    private const string Antigravity =
        "M12,1.4 C14.3,1.4 15.8,3.6 16.9,6.2 C18.3,9.6 19.1,13.2 20.1,16.2 C20.9,18.6 22.1,20.3 23.1,21.1 " +
        "C23.8,21.7 23.5,22.9 22.4,22.9 C21.7,22.9 21,22.9 20.2,22.6 C18.6,21.4 17.4,18.4 16.3,16.3 " +
        "C15.2,14.2 13.8,12.7 12,12.7 C10.2,12.7 8.8,14.2 7.7,16.3 C6.6,18.4 5.4,21.4 3.8,22.6 " +
        "C3,22.9 2.3,22.9 1.6,22.9 C0.5,22.9 0.2,21.7 0.9,21.1 C1.9,20.3 3.1,18.6 3.9,16.2 " +
        "C4.9,13.2 5.7,9.6 7.1,6.2 C8.2,3.6 9.7,1.4 12,1.4 Z";

    public static string PathData(ProviderKind provider) => provider switch
    {
        ProviderKind.Claude => Claude,
        ProviderKind.Codex => Codex,
        ProviderKind.Copilot => Copilot,
        ProviderKind.Antigravity => Antigravity,
        _ => throw new ArgumentOutOfRangeException(nameof(provider)),
    };
}

using System.Text.RegularExpressions;
using AiUsage.Features.Ledger;
using AiUsage.Features.Ledger.Contract;
using Xunit;

namespace AiUsage.Presentation.Tests;

/// <summary>R-07: every provider has its own monochrome mark, written as XAML path data.</summary>
public sealed class ProviderMarkTests
{
    [Fact]
    public void EveryProviderHasADistinctMark()
    {
        var marks = Enum.GetValues<ProviderKind>().Select(ProviderMarkData.PathData).ToArray();

        Assert.All(marks, data =>
        {
            Assert.False(string.IsNullOrWhiteSpace(data));
            Assert.Matches(new Regex(@"^(F[01] )?M[MmLlHhVvCcSsQqTtAaZz0-9 ,.\-]+$"), data);
        });
        Assert.Equal(marks.Length, marks.Distinct().Count());
    }
}

using Xunit;

namespace AiUsage.Infrastructure.Tests;

// One class per shard of the AuditScenarioTests corpus so xUnit runs the shards in parallel.

public sealed class AuditCorpusShard0Tests
{
    public static IEnumerable<object[]> Cases() => AuditScenarioTests.CorpusShard(0);

    [Theory]
    [MemberData(nameof(Cases))]
    public Task ParserRecorderBudgetAndProjectionMatchIndependentExpectations(string id) => AuditScenarioTests.VerifyCorpusCaseAsync(id);
}

public sealed class AuditCorpusShard1Tests
{
    public static IEnumerable<object[]> Cases() => AuditScenarioTests.CorpusShard(1);

    [Theory]
    [MemberData(nameof(Cases))]
    public Task ParserRecorderBudgetAndProjectionMatchIndependentExpectations(string id) => AuditScenarioTests.VerifyCorpusCaseAsync(id);
}

public sealed class AuditCorpusShard2Tests
{
    public static IEnumerable<object[]> Cases() => AuditScenarioTests.CorpusShard(2);

    [Theory]
    [MemberData(nameof(Cases))]
    public Task ParserRecorderBudgetAndProjectionMatchIndependentExpectations(string id) => AuditScenarioTests.VerifyCorpusCaseAsync(id);
}

public sealed class AuditCorpusShard3Tests
{
    public static IEnumerable<object[]> Cases() => AuditScenarioTests.CorpusShard(3);

    [Theory]
    [MemberData(nameof(Cases))]
    public Task ParserRecorderBudgetAndProjectionMatchIndependentExpectations(string id) => AuditScenarioTests.VerifyCorpusCaseAsync(id);
}

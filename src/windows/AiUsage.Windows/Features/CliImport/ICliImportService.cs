namespace AiUsage.Features.CliImport;

public enum CliImportOutcomeKind { Importing, Imported, Reimported, Duplicate, Unsupported, Failed, Cancelled }

/// <summary><paramref name="DisplayPath"/> is a sanitized location hint, never credential content.</summary>
public sealed record CliCandidate(string Id, string ProviderId, string Label, string DisplayPath, bool Importable);

/// <summary><paramref name="ExistingLabel"/> names the account a duplicate maps to.</summary>
public sealed record CliImportOutcome(string CandidateId, CliImportOutcomeKind Kind, string? AccountId = null, string? ExistingLabel = null);

/// <summary>
/// Adapter boundary for CLI discovery and import. Discovery starts only on explicit user request and yields candidates
/// progressively. Import yields one outcome per candidate; cancellation keeps completed items.
/// </summary>
public interface ICliImportService
{
    IAsyncEnumerable<CliCandidate> DiscoverAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<CliImportOutcome> ImportAsync(IReadOnlyList<string> candidateIds, bool reimport, CancellationToken cancellationToken);
}

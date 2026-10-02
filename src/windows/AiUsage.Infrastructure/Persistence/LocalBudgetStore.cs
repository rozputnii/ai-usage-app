using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiUsage.Core.Budget;
using AiUsage.Core.Usage;
using AiUsage.Infrastructure.Providers;

namespace AiUsage.Infrastructure.Persistence;

/// <summary>Versioned observation/configuration files. No provider state or credential access.</summary>
public sealed partial class LocalBudgetStore : IReadingSeriesStore, IBudgetConfigurationStore, IBudgetDataCleanup, IDisposable
{
    private const int SeriesBytes = 16 * 1024 * 1024;
    private const int ConfigurationBytes = 256 * 1024;
    private const int MaximumRuns = 25000;
    private const string ConfigurationName = "configuration.v1.json";
    private readonly string directory;
    private readonly BudgetJsonFile files;
    private readonly SemaphoreSlim gate = new(1);

    public LocalBudgetStore(string ownedRoot)
    {
        directory = Path.Combine(Path.GetFullPath(ownedRoot), "budget");
        files = new(directory);
    }

    public Task AppendAsync(IReadOnlyList<ReadingObservation> observations, CancellationToken token) => ExclusiveAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count > 1024) throw new ArgumentException("Too many observations.", nameof(observations));
        foreach (var observation in observations) ValidateObservation(observation);
        foreach (var group in observations.GroupBy(o => o.Series))
        {
            var read = await ReadSeriesAsync(group.Key, token).ConfigureAwait(false);
            var runs = read.Value.ToList();
            foreach (var observation in group.OrderBy(o => o.FetchedAt)) Add(runs, observation);
            if (runs.Count > 0)
            {
                var cutoff = runs[^1].LastConfirmed - DateTimeOffset.MinValue >= TimeSpan.FromDays(35)
                    ? runs[^1].LastConfirmed.AddDays(-35) : DateTimeOffset.MinValue;
                int first = runs.FindIndex(r => r.LastConfirmed >= cutoff);
                if (first > 1) runs.RemoveRange(0, first - 1);
            }
            if (runs.Count > MaximumRuns) throw new IOException("Series capacity reached.");
            await files.WriteAsync(SeriesName(group.Key), new SeriesDocument(1, group.Key, runs.Select(StoredRun.From).ToArray()), SeriesBytes, token).ConfigureAwait(false);
        }
    }, token);

    public Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadAsync(ReadingSeriesKey series, CancellationToken token) =>
        ExclusiveAsync(() => ReadSeriesAsync(series, token), token);

    private async Task<StoreRead<IReadOnlyList<ReadingRun>>> ReadSeriesAsync(ReadingSeriesKey series, CancellationToken token)
    {
        ValidateKey(series);
        var read = await files.ReadAsync(SeriesName(series), SeriesBytes, () => new SeriesDocument(1, series, []), document =>
        {
            if (document.Version != 1 || document.Series != series || document.Runs is null || document.Runs.Count > MaximumRuns)
                throw new InvalidDataException("Unsupported series.");
            DateTimeOffset? last = null;
            foreach (var stored in document.Runs)
            {
                if (stored is null || stored.Value is null || string.IsNullOrEmpty(stored.PeriodInstance) || stored.PeriodInstance.Length > 64 ||
                    stored.FirstSeen > stored.LastConfirmed || stored.FirstSeen <= last || !Enum.IsDefined(stored.Source) ||
                    stored.ResetPrecision is { } precision && !Enum.IsDefined(precision) || stored.UsedPercent is < 0 or > 100 ||
                    stored.PlanType?.Length > 4096 || stored.SourceVersion?.Length > 128 || stored.PeriodStartedAt > stored.FirstSeen || stored.RestartAfter >= stored.FirstSeen)
                    throw new InvalidDataException("Invalid observation run.");
                _ = stored.Value.ToQuantity();
                last = stored.LastConfirmed;
            }
        }, token).ConfigureAwait(false);
        return new(read.Value.Runs.Select(r => r.ToRun(series)).ToArray(), read.Recovered);
    }

    private static void Add(List<ReadingRun> runs, ReadingObservation observation)
    {
        var previous = runs.LastOrDefault();
        if (previous is not null && observation.FetchedAt <= previous.LastConfirmed) return;
        var next = new ReadingRun(observation.Series, observation.Value, observation.FetchedAt, observation.FetchedAt,
            previous?.PeriodInstance ?? Guid.NewGuid().ToString("N"), observation.PlanType, observation.Source)
        {
            ResetAt = observation.ResetAt, ResetPrecision = observation.ResetPrecision, UsedPercent = observation.UsedPercent,
            SourceVersion = observation.SourceVersion, IsBalance = observation.IsBalance,
            PeriodStartedAt = previous is null ? observation.PeriodStartedAt : previous.PeriodStartedAt, RestartAfter = previous?.RestartAfter
        };
        if (previous is not null)
        {
            bool comparable = SameScale(previous.Value, next.Value) && previous.IsBalance == next.IsBalance;
            var transition = observation.IsBalance ? new PeriodChange(PeriodChangeKind.Continuing) :
                ReadingCalculations.Transition(previous, next, observation.RoundingUnit);
            if (!comparable || transition.Kind is PeriodChangeKind.Rollover or PeriodChangeKind.EarlyReplenishment)
                next = next with { PeriodInstance = Guid.NewGuid().ToString("N"), PeriodStartedAt = comparable ? transition.StartedAt : null,
                    RestartAfter = comparable ? transition.After : null };
            if (next.PeriodInstance == previous.PeriodInstance && previous.Value == next.Value && previous.UsedPercent == next.UsedPercent &&
                previous.PlanType == next.PlanType && previous.Source == next.Source && previous.SourceVersion == next.SourceVersion &&
                previous.ResetPrecision == next.ResetPrecision && next.FirstSeen - previous.LastConfirmed <= TimeSpan.FromMinutes(15))
            {
                runs[^1] = previous with { LastConfirmed = next.LastConfirmed, ResetAt = next.ResetAt };
                return;
            }
        }
        runs.Add(next);
    }

    private static bool SameScale(Quantity a, Quantity b) => (a, b) switch
    {
        (CountQuantity x, CountQuantity y) => x.Unit == y.Unit,
        (MoneyQuantity x, MoneyQuantity y) => x.Currency == y.Currency && x.Exponent == y.Exponent,
        _ => false
    };

    public Task<StoreRead<BudgetConfiguration>> LoadConfigurationAsync(CancellationToken token) => ExclusiveAsync(() => LoadConfigurationCoreAsync(token), token);
    private async Task<StoreRead<BudgetConfiguration>> LoadConfigurationCoreAsync(CancellationToken token)
    {
        var read = await files.ReadAsync(ConfigurationName, ConfigurationBytes, () => ToDocument(BudgetConfiguration.Default), ValidateConfiguration, token).ConfigureAwait(false);
        return new(new(read.Value.WorkDays.ToArray(), read.Value.Caps.Select(c => new StoredPersonalCap(c.Series, new(c.Amount.ToQuantity(), c.SetAt))).ToArray()), read.Recovered);
    }
    public Task SaveConfigurationAsync(BudgetConfiguration configuration, CancellationToken token) => ExclusiveAsync(async () =>
    {
        var document = ToDocument(configuration);
        ValidateConfiguration(document);
        // Read first so a newer/corrupt file is preserved before replacement.
        _ = await LoadConfigurationCoreAsync(token).ConfigureAwait(false);
        await files.WriteAsync(ConfigurationName, document, ConfigurationBytes, token).ConfigureAwait(false);
    }, token);

    private static ConfigurationDocument ToDocument(BudgetConfiguration configuration) => new(1, configuration.WorkDays.ToArray(),
        configuration.Caps.Select(c => new CapDocument(c.Series, StoredQuantity.From(c.Cap.Amount), c.Cap.SetAt)).ToArray());
    private static void ValidateConfiguration(ConfigurationDocument document)
    {
        if (document.Version != 1 || document.WorkDays is null || document.Caps is null || document.WorkDays.Count > 7 ||
            document.WorkDays.Any(d => !Enum.IsDefined(d)) || document.WorkDays.Distinct().Count() != document.WorkDays.Count ||
            document.Caps.Count > 1024 || document.Caps.Any(c => c is null) || document.Caps.Select(c => c.Series).Distinct().Count() != document.Caps.Count)
            throw new InvalidDataException("Invalid budget configuration.");
        foreach (var cap in document.Caps)
        {
            ValidateKey(cap.Series);
            if (cap.Amount is null || cap.Amount.ToQuantity() is CountQuantity { Value: < 0 } or MoneyQuantity { MinorUnits: < 0 })
                throw new InvalidDataException("Invalid cap.");
        }
    }

    public Task DeleteAccountAsync(string accountTarget, CancellationToken token) => ExclusiveAsync(async () =>
    {
        ValidateText(accountTarget);
        _ = OwnedPaths(); // Validate the complete namespace before any mutation.
        var configuration = await LoadConfigurationCoreAsync(token).ConfigureAwait(false);
        var paths = OwnedPaths(); // Recovery can have just created a new quarantine.
        // Unparsed shared configuration may contain this target's caps and other targets'
        // caps too. Preserve it, and do not claim that selective deletion removed all data.
        if (paths.Any(p => Path.GetFileName(p).StartsWith(ConfigurationName + ".", StringComparison.Ordinal)))
            throw new IOException("Configuration recovery data requires whole-store cleanup.");
        await files.WriteAsync(ConfigurationName, ToDocument(configuration.Value with
        { Caps = configuration.Value.Caps.Where(c => c.Series.AccountTarget != accountTarget).ToArray() }), ConfigurationBytes, token).ConfigureAwait(false);
        foreach (var path in paths.Where(p => Path.GetFileName(p).StartsWith("series-" + Hash(accountTarget) + "-", StringComparison.Ordinal)))
        {
            token.ThrowIfCancellationRequested();
            files.Check(path);
            File.Delete(path);
        }
    }, token);

    public Task DeleteAllAsync(CancellationToken token) => ExclusiveAsync(() =>
    {
        var paths = OwnedPaths();
        foreach (var path in paths)
        {
            token.ThrowIfCancellationRequested();
            files.Check(path);
            File.Delete(path);
        }
        return Task.CompletedTask;
    }, token);

    private string[] OwnedPaths()
    {
        ProviderStatePaths.CheckDirectory(directory);
        var paths = Directory.GetFileSystemEntries(directory);
        foreach (var path in paths)
        {
            files.Check(path);
            if (Path.GetFileName(path) != "budget.lock" && !OwnedName().IsMatch(Path.GetFileName(path)))
                throw new IOException("Unknown budget data is preserved.");
        }
        return paths.Where(p => Path.GetFileName(p) != "budget.lock").ToArray();
    }

    [GeneratedRegex(@"\A(?:configuration\.v1|series-[A-F0-9]{64}-[A-F0-9]{64}\.v1)\.json(?:\.(?:quarantine|stage)-[a-f0-9]{32})?\z", RegexOptions.CultureInvariant)]
    private static partial Regex OwnedName();
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    private static string SeriesName(ReadingSeriesKey series) => "series-" + Hash(series.AccountTarget) + "-" + Hash(series.Limit) + ".v1.json";

    private async Task ExclusiveAsync(Func<Task> action, CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            using var lease = ProviderStatePaths.Acquire(directory, "budget.lock");
            await action().ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }
    private async Task<T> ExclusiveAsync<T>(Func<Task<T>> action, CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            using var lease = ProviderStatePaths.Acquire(directory, "budget.lock");
            return await action().ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }
    private static void ValidateObservation(ReadingObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ValidateKey(observation.Series);
        _ = StoredQuantity.From(observation.Value).ToQuantity();
        if (observation.RoundingUnit <= 0 || observation.UsedPercent is < 0 or > 100 || !Enum.IsDefined(observation.Source) ||
            observation.ResetPrecision is { } precision && !Enum.IsDefined(precision) || observation.PlanType?.Length > 4096 ||
            observation.SourceVersion?.Length > 128 || observation.PeriodStartedAt > observation.FetchedAt)
            throw new ArgumentException("Invalid observation.", nameof(observation));
    }
    private static void ValidateKey(ReadingSeriesKey series)
    {
        if (series is null || series.Limit is null) throw new InvalidDataException("Invalid series key.");
        ValidateText(series.AccountTarget);
        ValidateText(series.Limit.Provider);
        ValidateText(series.Limit.Family);
        if (series.Limit.NativeDiscriminator is null || series.Limit.NativeDiscriminator.Length > 4096) throw new InvalidDataException("Invalid limit key.");
    }
    private static void ValidateText(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 4096) throw new InvalidDataException("Invalid store key.");
    }
    public void Dispose() => gate.Dispose();
}

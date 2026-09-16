using System.Collections.ObjectModel;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.CliImport;

public enum CliStage { Idle, Scanning, Found, Importing, Done }

public enum OutcomeTone { Neutral, Positive, Critical }

internal sealed partial class CliCandidateViewModel(CliCandidate candidate) : ObservableObject
{
    public CliCandidate Candidate { get; } = candidate;
    public string Id => Candidate.Id;
    public string Label => Candidate.Label;
    public string Path => Candidate.DisplayPath;
    public string Glyph => Providers.Get(Candidate.ProviderId).Glyph;

    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] public partial bool IsEnabled { get; set; } = true;
    [ObservableProperty] public partial string OutcomeText { get; set; } = string.Empty;
    [ObservableProperty] public partial OutcomeTone Tone { get; set; }
    [ObservableProperty] public partial bool IsImporting { get; set; }
    [ObservableProperty] public partial CliImportOutcomeKind? Outcome { get; set; }

    public bool HasOutcome => OutcomeText.Length > 0;
    partial void OnOutcomeTextChanged(string value) => OnPropertyChanged(nameof(HasOutcome));
}

/// <summary>
/// S08 Import from CLI: explicit "Scan this PC", progressive candidates with Stop, selection, per-item outcomes,
/// cancellation that keeps completed items, and re-import mapping to the same identity (D-086–D-089).
/// </summary>
internal sealed partial class CliImportViewModel(PresentationContext context, ICliImportService service) : ObservableObject
{
    private CancellationTokenSource? operation;

    public ObservableCollection<CliCandidateViewModel> Candidates { get; } = [];

    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsScanning), nameof(ShowList), nameof(IsImporting), nameof(IsDone), nameof(NoCandidates), nameof(CanRescan), nameof(CanImport), nameof(ScanningText))]
    [NotifyCanExecuteChangedFor(nameof(ImportSelectedCommand), nameof(ReimportSelectedCommand), nameof(ScanCommand))]
    [ObservableProperty] public partial CliStage Stage { get; private set; }

    public bool IsIdle => Stage == CliStage.Idle;
    public bool IsScanning => Stage == CliStage.Scanning;
    public bool IsImporting => Stage == CliStage.Importing;
    public bool IsDone => Stage == CliStage.Done;
    public bool ShowList => Candidates.Count > 0 && Stage != CliStage.Idle;
    public bool NoCandidates => Stage == CliStage.Found && Candidates.Count == 0;
    public bool CanRescan => Stage is CliStage.Found or CliStage.Done;
    public int SelectedCount => Candidates.Count(c => c.IsSelected);
    public bool CanImport => Stage == CliStage.Found && SelectedCount > 0;
    public string ScanningText => context.Format.F("Cli_Scanning", Candidates.Count);
    public string ImportLabel => context.Format.F("Cli_ImportSelected", SelectedCount);

    public void Open()
    {
        if (Stage is CliStage.Scanning or CliStage.Importing)
            return;
        if (Stage == CliStage.Done)
            return;
        if (Candidates.Count == 0)
            Stage = CliStage.Idle;
    }

    private bool CanScan() => Stage is not (CliStage.Scanning or CliStage.Importing);

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        using var cancellation = Begin();
        foreach (var candidate in Candidates)
            candidate.PropertyChanged -= OnCandidateChanged;
        Candidates.Clear();
        Stage = CliStage.Scanning;
        context.Announcer.Announce(context.Format.T("Announce_CliScanning"));
        try
        {
            await foreach (var candidate in service.DiscoverAsync(cancellation.Token))
            {
                var item = new CliCandidateViewModel(candidate) { IsSelected = candidate.Importable, IsEnabled = true };
                item.PropertyChanged += OnCandidateChanged;
                Candidates.Add(item);
                OnPropertyChanged(nameof(ScanningText));
                OnPropertyChanged(nameof(ShowList));
            }
            Stage = CliStage.Found;
            context.Announcer.Announce(context.Format.F("Announce_CliFound", Candidates.Count));
        }
        catch (OperationCanceledException)
        {
            Stage = Candidates.Count > 0 ? CliStage.Found : CliStage.Idle;
            context.Announcer.Announce(context.Format.T("Announce_CliScanCancelled"));
        }
        finally { End(cancellation); }
        Changed();
    }

    [RelayCommand]
    private void Stop() => operation?.Cancel();

    [RelayCommand(CanExecute = nameof(CanImport))]
    private Task ImportSelectedAsync() => ImportAsync(reimport: false);

    private bool CanReimport() => Stage == CliStage.Done && SelectedCount > 0;

    [RelayCommand(CanExecute = nameof(CanReimport))]
    private Task ReimportSelectedAsync() => ImportAsync(reimport: true);

    [RelayCommand]
    private void CancelImport() => operation?.Cancel();

    private async Task ImportAsync(bool reimport)
    {
        var selected = Candidates.Where(c => c.IsSelected).ToArray();
        if (selected.Length == 0)
            return;
        using var cancellation = Begin();
        Stage = CliStage.Importing;
        foreach (var candidate in Candidates)
            candidate.IsEnabled = false;
        foreach (var candidate in selected)
        {
            candidate.IsImporting = true;
            candidate.OutcomeText = context.Format.T("Cli_Importing");
            candidate.Tone = OutcomeTone.Neutral;
        }
        try
        {
            await foreach (var outcome in service.ImportAsync(selected.Select(c => c.Id).ToArray(), reimport, cancellation.Token))
                Apply(outcome);
            context.Announcer.Announce(context.Format.T("Announce_CliImportFinished"));
        }
        catch (OperationCanceledException)
        {
            foreach (var candidate in selected.Where(c => c.IsImporting))
                Apply(new CliImportOutcome(candidate.Id, CliImportOutcomeKind.Cancelled));
            context.Announcer.Announce(context.Format.T("Announce_CliImportCancelled"));
        }
        finally
        {
            End(cancellation);
            foreach (var candidate in Candidates)
                candidate.IsEnabled = true;
            Stage = CliStage.Done;
            Changed();
        }
    }

    private void Apply(CliImportOutcome outcome)
    {
        var candidate = Candidates.FirstOrDefault(c => c.Id == outcome.CandidateId);
        if (candidate is null)
            return;
        var format = context.Format;
        candidate.IsImporting = false;
        candidate.Outcome = outcome.Kind;
        (candidate.OutcomeText, candidate.Tone) = outcome.Kind switch
        {
            CliImportOutcomeKind.Imported => (format.T("Cli_Imported"), OutcomeTone.Positive),
            CliImportOutcomeKind.Reimported => (format.T("Cli_Reimported"), OutcomeTone.Positive),
            CliImportOutcomeKind.Duplicate => (format.F("Cli_Duplicate", outcome.ExistingLabel ?? string.Empty), OutcomeTone.Neutral),
            CliImportOutcomeKind.Unsupported => (format.T("Cli_Unsupported"), OutcomeTone.Neutral),
            CliImportOutcomeKind.Cancelled => (format.T("Cli_Cancelled"), OutcomeTone.Neutral),
            CliImportOutcomeKind.Importing => (format.T("Cli_Importing"), OutcomeTone.Neutral),
            _ => (format.T("Cli_Failed"), OutcomeTone.Critical),
        };
        candidate.IsImporting = outcome.Kind == CliImportOutcomeKind.Importing;
    }

    private CancellationTokenSource Begin()
    {
        operation?.Cancel();
        var cancellation = new CancellationTokenSource();
        operation = cancellation;
        return cancellation;
    }

    private void End(CancellationTokenSource cancellation)
    {
        if (operation == cancellation)
            operation = null;
    }

    private void OnCandidateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CliCandidateViewModel.IsSelected))
            Changed();
    }

    private void Changed()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanImport));
        OnPropertyChanged(nameof(ImportLabel));
        OnPropertyChanged(nameof(ShowList));
        OnPropertyChanged(nameof(NoCandidates));
        ImportSelectedCommand.NotifyCanExecuteChanged();
        ReimportSelectedCommand.NotifyCanExecuteChanged();
    }
}

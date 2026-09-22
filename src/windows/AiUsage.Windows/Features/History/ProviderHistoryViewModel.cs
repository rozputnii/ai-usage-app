using System.Collections.ObjectModel;
using System.Globalization;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.History;

internal sealed record ProviderHistoryAccount(string Id, string Label);
internal sealed record ProviderHistoryRange(int Days, string Label);
internal sealed record ProviderHistoryRow(string Period, string Metric, string Value, string Details);
internal sealed record ProviderHistoryReport(string Title, string Status, IReadOnlyList<ProviderHistoryRow> Rows);

internal sealed partial class ProviderHistorySection(string id, string label) : ObservableObject
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsStale { get; set; }
    [ObservableProperty] public partial string Message { get; set; } = "";
    [ObservableProperty] public partial string Retrieved { get; set; } = "";
    [ObservableProperty] public partial IReadOnlyList<ProviderHistoryReport> Reports { get; set; } = [];
}

/// <summary>Remote history is requested by navigation and explicit user actions, never the clock.</summary>
internal sealed partial class ProviderHistoryViewModel : ObservableObject, IDisposable
{
    private readonly PresentationContext context;
    private readonly IProviderHistorySource source;
    private readonly IDisposable subscription;
    private readonly Dictionary<(string Account, HistoryRange Range), ProviderHistoryResult> cache = [];
    private CancellationTokenSource? cancellation;
    private string accountState = "";
    private string? loadingKey;
    private bool active;
    private bool applying;
    private bool disposed;
    private int generation;

    public ProviderHistoryViewModel(PresentationContext context, IProviderHistorySource source)
    {
        this.context = context; this.source = source;
        Ranges = [new(7, context.Format.T("Range_7d")), new(30, context.Format.T("Range_30d")),
            new(90, context.Format.T("Range_90d")), new(365, context.Format.T("Range_1y")), new(0, context.Format.T("Range_Custom"))];
        SelectedRange = Ranges[1];
        var today = DateOnly.FromDateTime(context.Clock.UtcNow.UtcDateTime);
        CustomFrom = today.AddDays(-29).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        CustomTo = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        subscription = context.Usage.Subscribe(snapshot => Dispatch(() => UpdateAccounts(snapshot)));
        context.Navigation.Navigated += OnNavigation;
        UpdateAccounts(context.Usage.Current);
    }

    public ObservableCollection<ProviderHistoryAccount> Accounts { get; } = [];
    public ObservableCollection<ProviderHistorySection> Sections { get; } = [];
    public IReadOnlyList<ProviderHistoryRange> Ranges { get; }
    public Task Pending { get; private set; } = Task.CompletedTask;
    [ObservableProperty] public partial ProviderHistoryAccount? SelectedAccount { get; set; }
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    [ObservableProperty] public partial ProviderHistoryRange? SelectedRange { get; set; }
    [ObservableProperty] public partial string CustomFrom { get; set; }
    [ObservableProperty] public partial string CustomTo { get; set; }
    [ObservableProperty] public partial string Error { get; private set; } = "";
    [ObservableProperty] public partial bool HasNoAccounts { get; private set; }
    public bool IsCustom => SelectedRange?.Days == 0;
    public string Subtitle => context.Format.T("ProviderHistory_Subtitle");

    partial void OnSelectedAccountChanged(ProviderHistoryAccount? value) { if (!applying && active) _ = LoadAsync(); }
    partial void OnSelectedRangeChanged(ProviderHistoryRange? value) { if (!applying && active && value?.Days > 0) _ = LoadAsync(); }
    [RelayCommand] private Task RefreshAsync() => LoadAsync(force: true);
    [RelayCommand] private Task ApplyAsync() => LoadAsync();

    private void OnNavigation(object? sender, NavigationRequest request)
    {
        if (request.Page != PageKey.History)
        {
            active = false; cancellation?.Cancel(); loadingKey = null;
            foreach (var section in Sections) section.IsLoading = false;
            return;
        }
        active = true;
        applying = true;
        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == (request.AccountId ?? "")) ?? Accounts.FirstOrDefault();
        applying = false;
        _ = LoadAsync();
    }

    public void Activate() { if (!active) { active = true; _ = LoadAsync(); } }

    private void UpdateAccounts(UiSnapshot snapshot)
    {
        if (disposed) return;
        var accounts = snapshot.Accounts.Where(a => a.Connection != ConnectionState.NotConnected).ToArray();
        var state = string.Join("|", accounts.Select(a => a.Id + ":" + a.Connection + ":" +
            (a.Operation is AccountOperation.Loading or AccountOperation.Disconnecting ? a.Operation.ToString() : "")));
        var changed = state != accountState;
        if (changed)
        {
            generation++;
            cancellation?.Cancel(); loadingKey = null;
            foreach (var key in cache.Keys.ToArray())
                if (!accounts.Any(a => a.Id == key.Account && a.Connection == ConnectionState.Connected &&
                    a.Operation is not (AccountOperation.Loading or AccountOperation.Disconnecting))) cache.Remove(key);
            Sections.Clear();
        }
        accountState = state;
        applying = true;
        var selected = SelectedAccount?.Id ?? "";
        CollectionSync.Sync(Accounts, new[] { new ProviderHistoryAccount("", context.Format.T("ProviderHistory_AllAccounts")) }
            .Concat(accounts.Select(a => new ProviderHistoryAccount(a.Id, a.Label))).ToArray(), a => a.Id + a.Label, a => a.Id + a.Label, a => a, (_, _) => { });
        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == selected) ?? Accounts[0];
        applying = false;
        HasNoAccounts = accounts.Length == 0;
        if (active && changed) _ = LoadAsync();
    }

    public Task LoadAsync(bool force = false)
    {
        if (disposed || !active) return Task.CompletedTask;
        var today = DateOnly.FromDateTime(context.Clock.UtcNow.UtcDateTime);
        HistoryRange range;
        if (IsCustom)
        {
            if (!DateOnly.TryParseExact(CustomFrom, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from) ||
                !DateOnly.TryParseExact(CustomTo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to) || from > to || to > today || to.DayNumber - from.DayNumber > 730)
            { Error = context.Format.T("ProviderHistory_InvalidDates"); return Task.CompletedTask; }
            range = new(from, to);
        }
        else range = new(today.AddDays(-(SelectedRange?.Days ?? 30) + 1), today);
        Error = "";
        var selected = SelectedAccount?.Id ?? "";
        var key = $"{generation}:{selected}:{range.From}:{range.To}";
        if (!force && key == loadingKey) return Pending;
        cancellation?.Cancel(); cancellation?.Dispose();
        cancellation = new();
        loadingKey = key;
        var accounts = Accounts.Where(a => a.Id.Length != 0 && (selected.Length == 0 || a.Id == selected)).ToArray();
        Sections.Clear();
        foreach (var account in accounts) Sections.Add(new(account.Id, account.Label));
        return Pending = LoadCoreAsync(range, force, cancellation.Token);
    }

    private async Task LoadCoreAsync(HistoryRange range, bool force, CancellationToken token)
    {
        // Publish Pending before a synchronously completing source can trigger another operation.
        await Task.Yield();
        if (token.IsCancellationRequested) return;
        await Task.WhenAll(Sections.ToArray().Select(section => LoadSectionAsync(section, range, force, token)));
    }

    private async Task LoadSectionAsync(ProviderHistorySection section, HistoryRange range, bool force, CancellationToken token)
    {
        var key = (section.Id, range);
        cache.TryGetValue(key, out var previous);
        if (previous is not null) Present(section, previous);
        if (!force && previous is not null && context.Clock.UtcNow - previous.FetchedAt < TimeSpan.FromMinutes(5)) return;
        section.IsLoading = true;
        try
        {
            var result = await source.GetHistoryAsync(section.Id, range, token);
            if (token.IsCancellationRequested || disposed) return;
            if (result.Reports.Any(r => r.Status == HistoryStatus.AccountChanged))
            {
                foreach (var oldKey in cache.Keys.Where(k => k.Account == section.Id).ToArray()) cache.Remove(oldKey);
                previous = null;
            }
            var failure = result.Reports.Any(r => r.Status is not (HistoryStatus.Available or HistoryStatus.Empty));
            if (previous is not null && failure)
            {
                // Retain only matching report IDs from the same account/range; keep the failure visible.
                result = result with { Reports = result.Reports.Select(r => r.Status is HistoryStatus.Available or HistoryStatus.Empty ? r :
                    previous.Reports.FirstOrDefault(p => p.Id == r.Id) is { } old ? r with { Values = old.Values } : r).ToArray() };
                if (result.Reports.All(r => r.Values.Count == 0) && result.Reports.Count == 1 && result.Reports[0].Id == "history")
                    result = result with { Reports = previous.Reports.Select(r => r with { Status = result.Reports[0].Status }).ToArray() };
                section.IsStale = true;
            }
            if (!failure) cache[key] = result;
            Present(section, result, section.IsStale ? previous?.FetchedAt : null);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (!token.IsCancellationRequested) { section.IsStale = previous is not null; section.Message = context.Format.T("ProviderHistory_Failed"); }
        }
        finally { section.IsLoading = false; }
    }

    private void Present(ProviderHistorySection section, ProviderHistoryResult result, DateTimeOffset? retainedAt = null)
    {
        section.Reports = result.Reports.Select(r => new ProviderHistoryReport(
            ReportTitle(r.Id), Status(r.Status), r.Values.OrderByDescending(v => v.From).Select(Row).ToArray())).ToArray();
        section.Message = string.Join(" · ", result.Reports.Select(r => Status(r.Status)).Distinct());
        section.Retrieved = context.Format.F("ProviderHistory_Retrieved", context.Format.DateTime(retainedAt ?? result.FetchedAt));
        if (section.IsStale) section.Retrieved += " · " + context.Format.T("ProviderHistory_Stale");
    }

    private ProviderHistoryRow Row(HistoryValue value) => new(
        value.From == value.To ? value.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : $"{value.From:yyyy-MM-dd} – {value.To:yyyy-MM-dd}",
        MetricLabel(value.Metric), (value.Value?.ToString("0.############################", CultureInfo.CurrentCulture) ?? context.Format.T("ProviderHistory_Unknown")) + " " + (value.Unit ?? context.Format.T("ProviderHistory_UnknownUnit")),
        string.Join(" · ", value.Dimensions.Select(d => d.Key + ": " + d.Value)));

    private string Status(HistoryStatus status) => context.Format.T("ProviderHistory_" + status);
    private string ReportTitle(string id) => context.Format.T("ProviderHistory_Report_" + id.Split(':')[0]);
    private static string MetricLabel(string id) => id switch
    {
        "uncached_text_input_tokens" => "Input tokens", "cached_text_input_tokens" => "Cached input tokens", "text_output_tokens" => "Output tokens",
        "text_total_tokens" => "Text tokens", "total_tokens" => "Total tokens", "cost_usd" => "Cost", "credit_amount" => "Credit change",
        "grossQuantity" => "Gross usage", "discountQuantity" => "Discounted usage", "netQuantity" => "Net usage", "pricePerUnit" => "Unit price",
        "grossAmount" => "Gross amount", "discountAmount" => "Discount amount", "netAmount" => "Net amount", "invocation_counts" => "Invocations",
        _ => id.Replace('_', ' ')
    };
    private void Dispatch(Action action) { if (context.Dispatcher.HasThreadAccess) action(); else context.Dispatcher.Post(action); }
    public void Dispose()
    {
        disposed = true; cancellation?.Cancel(); cancellation?.Dispose(); cache.Clear();
        subscription.Dispose(); context.Navigation.Navigated -= OnNavigation;
    }
}

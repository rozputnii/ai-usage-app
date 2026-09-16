using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;
using AiUsage.Features.Accounts;
using AiUsage.Features.CliImport;
using AiUsage.Features.Connection;
using AiUsage.Features.Demo;
using AiUsage.Features.History;
using AiUsage.Features.Overview;
using AiUsage.Features.Presentation;
using AiUsage.Features.Recovery;
using AiUsage.Features.Settings;
using AiUsage.Features.Settings.Appearance;
using AiUsage.Features.Settings.DataPrivacy;
using AiUsage.Features.Settings.Monitoring;
using AiUsage.Features.Settings.Updates;
using AiUsage.Features.Shell;
using AiUsage.Features.SystemStatusPage;
using AiUsage.Features.Tray;

namespace AiUsage.Presentation.Tests;

internal static class Repository
{
    public static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CONTRIBUTING.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Run from a repository build output.");
    }
}

/// <summary>Real English strings from Resources.resw; a missing key fails the test instead of rendering silently.</summary>
internal sealed class TestText : ITextResources
{
    private static readonly Lazy<Dictionary<string, string>> Values = new(() =>
        XDocument.Load(Path.Combine(Repository.Root(), "src/windows/AiUsage.Windows/Strings/en-US/Resources.resw"))
            .Root!.Elements("data").ToDictionary(e => (string)e.Attribute("name")!, e => (string)e.Element("value")!));

    public static IReadOnlyDictionary<string, string> All => Values.Value;

    public string Get(string key) => Values.Value.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException("Missing resource: " + key);
}

/// <summary>Deterministic latency: delays stay pending until <see cref="Advance"/> releases them (or complete at once in auto mode).</summary>
internal sealed class ManualDelays
{
    private readonly List<(TaskCompletionSource Completion, CancellationTokenRegistration Registration)> pending = [];

    public bool Auto { get; set; } = true;
    public int Pending => pending.Count;

    public Task Delay(TimeSpan duration, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (Auto)
            return Task.CompletedTask;
        var completion = new TaskCompletionSource();
        var registration = token.Register(() => completion.TrySetCanceled(token));
        pending.Add((completion, registration));
        return completion.Task;
    }

    /// <summary>Completes every delay pending now, then lets continuations run.</summary>
    public async Task Advance(int rounds = 1)
    {
        for (var i = 0; i < rounds; i++)
        {
            var now = pending.ToArray();
            pending.Clear();
            foreach (var (completion, registration) in now)
            {
                registration.Dispose();
                completion.TrySetResult();
            }
            await Task.Yield();
        }
    }

    /// <summary>Releases delays until nothing is pending (bounded).</summary>
    public async Task Drain(int max = 200)
    {
        for (var i = 0; i < max && pending.Count > 0; i++)
            await Advance();
        await Task.Yield();
    }
}

internal sealed class TestDispatcher : IUiDispatcher
{
    public int Posts { get; private set; }
    public bool HasThreadAccess => true;
    public void Post(Action action)
    {
        Posts++;
        action();
    }
}

internal sealed class TestAnnouncer : IAnnouncer
{
    public List<string> Messages { get; } = [];
    public void Announce(string message) => Messages.Add(message);
    public string Last => Messages.LastOrDefault() ?? string.Empty;
}

internal sealed class TestNavigation : INavigationService
{
    private readonly Stack<NavigationRequest> back = new();
    public PageKey Current { get; private set; } = PageKey.Overview;
    public NavigationRequest? Last { get; private set; } = new(PageKey.Overview);
    public bool CanGoBack => back.Count > 0;
    public event EventHandler<NavigationRequest>? Navigated;

    public void Navigate(NavigationRequest request)
    {
        if (Last is not null)
            back.Push(Last);
        Last = request;
        Current = request.Page;
        Navigated?.Invoke(this, request);
    }

    public void GoBack()
    {
        if (back.Count == 0)
            return;
        Last = back.Pop();
        Current = Last.Page;
        Navigated?.Invoke(this, Last);
    }
}

/// <summary>Scripted dialogs. Confirming runs the request's ConfirmAction like the real dialog's busy state.</summary>
internal sealed class TestDialogs : IDialogService
{
    public Queue<(ConfirmOutcome Outcome, string? Typed)> Script { get; } = new();
    public List<ConfirmRequest> Requests { get; } = [];
    public List<AddAccountEntry> AddAccount { get; } = [];
    public List<string?> ActionErrors { get; } = [];
    public bool IsDialogOpen { get; set; }

    public void Next(ConfirmOutcome outcome, string? typed = null) => Script.Enqueue((outcome, typed));

    public async Task<ConfirmOutcome> ConfirmAsync(ConfirmRequest request)
    {
        Requests.Add(request);
        var (outcome, typed) = Script.Count > 0 ? Script.Dequeue() : (ConfirmOutcome.Cancelled, null);
        if (outcome == ConfirmOutcome.Confirmed && request.TypedConfirmation is { } required && typed != required)
            return ConfirmOutcome.Cancelled;
        var action = outcome switch
        {
            ConfirmOutcome.Confirmed => request.ConfirmAction,
            ConfirmOutcome.Alternate => request.AlternateAction,
            _ => null,
        };
        if (action is not null)
        {
            var error = await action(CancellationToken.None);
            ActionErrors.Add(error);
            if (error is not null)
                return ConfirmOutcome.Cancelled;
        }
        return outcome;
    }

    public Task ShowAddAccountAsync(AddAccountEntry entry)
    {
        AddAccount.Add(entry);
        return Task.CompletedTask;
    }
}

internal sealed class TestTheme : IThemeService
{
    public ThemePreference Preference { get; private set; }
    public EffectiveTheme SystemTheme { get; set; } = EffectiveTheme.Light;
    public EffectiveTheme Effective => Preference switch { ThemePreference.Light => EffectiveTheme.Light, ThemePreference.Dark => EffectiveTheme.Dark, _ => SystemTheme };
    public bool HighContrast { get; set; }
    public List<ThemePreference> Applied { get; } = [];
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Apply(ThemePreference preference)
    {
        Preference = preference;
        Applied.Add(preference);
        PropertyChanged?.Invoke(this, new(nameof(Effective)));
    }

    public void ChangeSystem(EffectiveTheme theme)
    {
        SystemTheme = theme;
        PropertyChanged?.Invoke(this, new(nameof(SystemTheme)));
    }
}

internal sealed class TestMotion : IMotionSettings
{
    public bool ReducedMotion { get; set; }
    public bool WindowVisible { get; set; } = true;
    public bool AnimationsAllowed => !ReducedMotion && WindowVisible;
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Raise() => PropertyChanged?.Invoke(this, new(nameof(AnimationsAllowed)));
}

internal sealed class TestLifetime : IAppLifetime
{
    public int Shown { get; private set; }
    public int Exits { get; private set; }
    public int PopupShown { get; private set; }
    public bool? AlwaysOnTop { get; private set; }
    public bool IsHiddenToTray { get; private set; }
    public event EventHandler? VisibilityChanged;
    public void ShowMainWindow() { Shown++; IsHiddenToTray = false; VisibilityChanged?.Invoke(this, EventArgs.Empty); }
    public void HideToTray() { IsHiddenToTray = true; VisibilityChanged?.Invoke(this, EventArgs.Empty); }
    public Task ExitAsync() { Exits++; return Task.CompletedTask; }
    public void SetAlwaysOnTop(bool value) => AlwaysOnTop = value;
    public void ShowTrayPopup() => PopupShown++;
}

internal sealed class TestDisplay : IDisplaySimulation
{
    public EffectiveTheme? SimulatedSystemTheme { get; set; }
    public bool SimulatedHighContrast { get; set; }
    public bool? ReducedMotionOverride { get; set; }
    public double ContentScale { get; set; } = 1;
    public bool ProviderHues { get; set; }
    public (int Width, int Height)? Resized { get; private set; }
    public void ResizeWindow(int effectiveWidth, int effectiveHeight) => Resized = (effectiveWidth, effectiveHeight);
}

/// <summary>Demo composition for tests: same mock services the app registers, with fakes for the WinUI host.</summary>
internal sealed class TestHost : IDisposable
{
    private readonly List<IDisposable> disposables = [];

    public TestHost(string scenario = "F02", bool autoDelays = true)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        Delays = new ManualDelays { Auto = autoDelays };
        Clock = new DemoClock(Delays.Delay, TimeZoneInfo.Utc);
        State = new DemoState(Clock, scenario);
        Usage = new DemoUsageSource(State);
        Connection = new DemoConnectionFlow(State);
        History = new DemoHistorySource(State);
        Preferences = new DemoPreferenceStore(State);
        NotificationPreview = new DemoNotificationPreview(State);
        Data = new DemoDataManagementService(State);
        Updates = new DemoUpdateService(State);
        Cli = new DemoCliImportService(State);
        Diagnostics = new DemoDiagnosticsService(State);
        Recovery = new DemoRecoveryService(State);
        Context = new PresentationContext(Usage, Dispatcher, Clock, Text, Announcer, Navigation, Dialogs, Motion);
        Controller = new DemoScenarioController(State, Connection, Updates, Cli, Navigation, Dialogs);
    }

    public ManualDelays Delays { get; }
    public DemoClock Clock { get; }
    public DemoState State { get; }
    public DemoUsageSource Usage { get; }
    public DemoConnectionFlow Connection { get; }
    public DemoHistorySource History { get; }
    public DemoPreferenceStore Preferences { get; }
    public DemoNotificationPreview NotificationPreview { get; }
    public DemoDataManagementService Data { get; }
    public DemoUpdateService Updates { get; }
    public DemoCliImportService Cli { get; }
    public DemoDiagnosticsService Diagnostics { get; }
    public DemoRecoveryService Recovery { get; }
    public DemoScenarioController Controller { get; }
    public TestText Text { get; } = new();
    public TestDispatcher Dispatcher { get; } = new();
    public TestAnnouncer Announcer { get; } = new();
    public TestNavigation Navigation { get; } = new();
    public TestDialogs Dialogs { get; } = new();
    public TestTheme Theme { get; } = new();
    public TestMotion Motion { get; } = new();
    public TestLifetime Lifetime { get; } = new();
    public TestDisplay Display { get; } = new();
    public PresentationContext Context { get; }
    public PresentationFormatter Format => Context.Format;

    private T Track<T>(T value)
    {
        if (value is IDisposable disposable)
            disposables.Add(disposable);
        return value;
    }

    public OverviewViewModel Overview() => Track(new OverviewViewModel(Context, History, Preferences));
    public AccountsViewModel Accounts() => Track(new AccountsViewModel(Context, History, Data, Preferences));
    public AddAccountViewModel AddAccount() => new(Context, Connection, new CliImportViewModel(Context, Cli), Controller);
    public CliImportViewModel CliImport() => new(Context, Cli);
    public HistoryViewModel HistoryPage() => Track(new HistoryViewModel(Context, History));
    public AppearanceSettingsViewModel Appearance() => Track(new AppearanceSettingsViewModel(Context, Preferences, Theme));
    public ToastViewModel Toast { get => field ??= new ToastViewModel(Context); }
    public MonitoringSettingsViewModel Monitoring() => Track(new MonitoringSettingsViewModel(Context, Preferences, NotificationPreview, Toast, Controller));
    public DataPrivacyViewModel DataPrivacy() => Track(new DataPrivacyViewModel(Context, Preferences, Data));
    public UpdatesViewModel UpdatesPage() => Track(new UpdatesViewModel(Context, Updates, Controller));
    public SystemStatusViewModel SystemStatus() => Track(new SystemStatusViewModel(Context, Diagnostics, () => Task.CompletedTask));
    public RecoveryViewModel RecoveryPage() => Track(new RecoveryViewModel(Context, Recovery));
    public ShellViewModel Shell() => Track(new ShellViewModel(Context, Theme, Lifetime, Toast, isDemo: true));
    public TrayViewModel Tray(ShellViewModel shell) => Track(new TrayViewModel(Context, Lifetime, shell.RefreshAllFromTrayAsync, () => shell.ExitCommand.ExecuteAsync(null)));
    public DemoControlViewModel DemoControl() => new(Controller, Display, Lifetime, Format);

    public AccountItem Account(string id) => Usage.Current.Accounts.Single(a => a.Id == id);

    public WindowItem Window(string accountId, string windowId) =>
        Account(accountId).Contexts.SelectMany(c => c.Groups).SelectMany(g => g.Windows).First(w => w.Id == windowId);

    public void Dispose()
    {
        foreach (var disposable in disposables)
            disposable.Dispose();
    }
}

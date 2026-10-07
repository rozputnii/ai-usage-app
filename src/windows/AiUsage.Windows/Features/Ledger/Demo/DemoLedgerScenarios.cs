using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Features.Ledger.Demo;

/// <summary>
/// Synthetic scenarios for the new presentation (spec section 5). Every account, figure and time is invented and follows the
/// AIU-034 design reference: the design-brief section 4 scenario as the reference draws it, every Provider States card, the
/// last work day, a day off with and without Work today, first run and sign-in. Figures stand for engine output; nothing
/// here computes a budget.
/// </summary>
internal static class DemoLedgerScenarios
{
    public const string Brief = "brief";
    public const string States = "states";
    public const string LastWorkDay = "last-work-day";
    public const string DayOff = "day-off";
    public const string FirstRun = "first-run";
    public const string SignIn = "sign-in";
    public const string Monetary = "monetary";
    public const string MonetaryWithWindows = "monetary-windows";
    public const string WorkBudget = "work-budget";

    public static IReadOnlyList<(string Id, string Title)> All { get; } =
    [
        (Brief, "Brief scenario · Wed 14 Oct"),
        (States, "Every card state"),
        (LastWorkDay, "Last work day · Fri 30 Oct"),
        (DayOff, "Day off · Sat 17 Oct"),
        (FirstRun, "First run"),
        (SignIn, "Sign-in strip and expired sign-in"),
        (Monetary, "Account spending"),
        (MonetaryWithWindows, "Account spending with new windows"),
        (WorkBudget, "Monthly work budget and subscription"),
    ];

    private static readonly TimeSpan Bst = TimeSpan.FromHours(1);
    private static readonly TimeSpan Gmt = TimeSpan.Zero;

    public static DateTimeOffset At(int month, int day, int hour, int minute) =>
        new(2026, month, day, hour, minute, 0, month < 10 || month == 10 && day < 25 ? Bst : Gmt);

    public static DateTimeOffset BriefNow => At(10, 14, 14, 20);

    public static LedgerSnapshot Build(string id, bool workToday = false) => id switch
    {
        States => StatesSnapshot(),
        LastWorkDay => LastWorkDaySnapshot(),
        DayOff => DayOffSnapshot(workToday),
        FirstRun => Empty(BriefNow),
        SignIn => SignInSnapshot(),
        Monetary => MonetarySnapshot(),
        MonetaryWithWindows => MonetarySnapshot(true),
        WorkBudget => WorkBudgetSnapshot(),
        _ => BriefSnapshot(),
    };

    private static LedgerSnapshot WorkBudgetSnapshot()
    {
        var snapshot = BriefSnapshot();
        var money = new LimitCardModel("work-month", "Spending", CardLayout.Pool, ScaleModel.Money("USD", 2),
            PeriodModel.Month(true), CardState.OnTrack, Freshness.Fresh(), [],
            new(29.66m, 29.66m, 181.22m, 90.90m, LimitValue.Known(2000), 2000, null, null, null),
            null, AssumedMonth, null, "work-month", CardAction.None, null)
        {
            Monetary = new(new(2966, 2, "USD"), new(200000, 2, "USD"), LimitValueKind.Known, null, true,
                "Monthly work budget · provider period unknown · calendar month assumed", null)
        };
        var codex = snapshot.Accounts.First(a => a.Provider == ProviderKind.Codex);
        return snapshot with { Accounts = [new("work-account", ProviderKind.Claude, "Claude Work", AccountHealth.Ok,
            BriefNow, null, null, [money]), codex with { DisplayName = "Codex subscription", Cards = [codex.Cards[0]] }] };
    }
    private static LedgerSnapshot MonetarySnapshot(bool withWindows = false)
    {
        var snapshot = BriefSnapshot();
        var money = new LimitCardModel("money-mixed", "Spending", CardLayout.Pool, ScaleModel.Money("USD", 2),
            PeriodModel.Month(true), CardState.OnTrack, Freshness.Fresh(), [],
            new(110, 100, 125, 20, LimitValue.Known(500), 300, null, null, null), null, AssumedMonth,
            new(300, true, CapStatus.Applied), "money-mixed", CardAction.None, null)
        {
            Monetary = new(new(11000, 2, "USD"), new(50000, 2, "USD"), LimitValueKind.Known, new(30000, 2, "USD"), true,
                "Account spending · provider period unknown · calendar month assumed", null)
        };
        var unknown = money with
        {
            CardId = "money-only", Layout = CardLayout.Note, State = CardState.PeriodUnknown,
            Figures = LimitFigures.UsedOnly(12.50m) with { ProviderLimit = LimitValue.Known(200) },
            Cap = null, CapTargetId = null,
            Monetary = new(new(1250, 2, "EUR"), new(20000, 2, "EUR"), LimitValueKind.Known, null, true,
                "Spending scope unverified · may be shared · provider period unknown",
                "Budget and cap editing unavailable until spending scope is established")
        };
        return snapshot with { Accounts =
        [
            snapshot.Accounts[0] with { DisplayName = "Mixed account", Cards = [snapshot.Accounts[0].Cards[0],
                Week("money-fable", CardState.OnTrack, 0, 0, 14, 20, At(10, 19, 9, 0), "Fable") with { ModelScoped = true }, money] },
            new("money-account", ProviderKind.Claude, "Money only", AccountHealth.Ok, BriefNow, null, null,
                withWindows ? [snapshot.Accounts[0].Cards[0] with { CardId = "money-window" }, unknown] : [unknown])
        ] };
    }

    // ---- Shared card builders (reference defaults) ----

    private static readonly ResetModel AssumedMonth = new(At(11, 1, 0, 0), new DateOnly(2026, 11, 1), ResetProvenance.Assumed, new DateOnly(2026, 10, 1));
    private static readonly ResetModel CopilotReset = new(null, new DateOnly(2026, 11, 1), ResetProvenance.Provider, new DateOnly(2026, 10, 1));

    private static ResetModel Reset(DateTimeOffset at) => new(at, null, ResetProvenance.Provider, null);

    public static LimitCardModel FiveHour(string id, CardState state, decimal u0, decimal u, decimal t, decimal cwU, DateTimeOffset? winEnd, DateTimeOffset reset,
        decimal? ws = 12, bool started = true, int? fit = null, string? scope = null, Freshness? freshness = null, IReadOnlyList<CardMark>? marks = null,
        CardAction action = CardAction.None, DayOffPreview? dayOff = null, decimal? usual = null, decimal? low = null, decimal? high = null, bool rough = false)
    {
        // A rough count runs from the high bound to the low bound; a settled one uses the share.
        int? Left(decimal? share) => share is { } s && s > 0 && u < 100 ? (int)Math.Floor((100 - u) / s) : null;
        return new(id, scope, CardLayout.FiveHourAndPeriod, ScaleModel.Percent, PeriodModel.Week, state, freshness ?? Freshness.Fresh(), marks ?? [],
            new LimitFigures(u, u0, t, usual, LimitValue.Known(100), 100, null, null, null),
            new FiveHourModel(ws, cwU, started, winEnd, Left(rough ? high : ws), fit)
            {
                WindowShareLow = low, WindowShareHigh = high, WindowsLeftMax = rough ? Left(low) : null, Rough = rough, Windows = low is null ? 0 : 3
            },
            Reset(reset), null, null, action, dayOff);
    }

    public static LimitCardModel Week(string id, CardState state, decimal u0, decimal u, decimal t, decimal? usual, DateTimeOffset reset, string? scope = null,
        Freshness? freshness = null, IReadOnlyList<CardMark>? marks = null, DayOffPreview? dayOff = null, FiveHourModel? fiveHour = null) =>
        new(id, scope, fiveHour is null ? CardLayout.Period : CardLayout.FiveHourAndPeriod, ScaleModel.Percent, PeriodModel.Week, state, freshness ?? Freshness.Fresh(), marks ?? [],
            new LimitFigures(u, u0, t, usual, LimitValue.Known(100), 100, null, null, null), fiveHour, Reset(reset), null, null, CardAction.None, dayOff);

    public static LimitCardModel Credits(string id, CardState state, decimal u0, decimal u, decimal t, decimal cap, decimal usual, decimal balance,
        DateOnly trackedSince, IReadOnlyList<CardMark>? marks = null) =>
        new(id, "credits", CardLayout.Pool, ScaleModel.Count("credits"), PeriodModel.Month(true), state, Freshness.Fresh(), marks ?? [],
            new LimitFigures(u, u0, t, usual, LimitValue.Unknown, cap, null, balance, new TrackingModel(trackedSince, false)),
            null, AssumedMonth, new CapModel(cap, true, CapStatus.Applied), id, CardAction.None, null);

    public static LimitCardModel Money(string id, CardState state, decimal u0, decimal u, decimal t, decimal cap, decimal providerLimit, decimal usual,
        string scope = "extra usage", IReadOnlyList<CardMark>? marks = null) =>
        new(id, scope, CardLayout.Pool, ScaleModel.Money("USD", 2), PeriodModel.Month(true), state, Freshness.Fresh(), marks ?? [],
            new LimitFigures(u, u0, t, usual, LimitValue.Known(providerLimit), Math.Min(cap, providerLimit), null, null, new TrackingModel(null, false)),
            null, AssumedMonth, new CapModel(cap, cap <= providerLimit, CapStatus.Applied), id, CardAction.None, null);

    public static LimitCardModel Requests(string id, string scope, CardState state, decimal u0, decimal u, decimal t, decimal limit, decimal usual,
        decimal? remaining, decimal? cap = null, bool unlimited = false) =>
        new(id, scope, CardLayout.Pool, ScaleModel.Count("requests"), PeriodModel.Month(true), state, Freshness.Fresh(), [],
            new LimitFigures(u, u0, t, usual, unlimited ? LimitValue.Unlimited : LimitValue.Known(limit), cap is { } c ? Math.Min(c, unlimited ? c : limit) : limit, remaining, null, null),
            null, CopilotReset, cap is { } amount ? new CapModel(amount, true, CapStatus.Applied) : null, id, CardAction.None, null);

    public static LimitCardModel Note(string id, string? scope, CardState state, ScaleModel scale, PeriodModel period, ResetModel? reset, decimal? used = null,
        CardAction action = CardAction.None, LimitValue? limit = null, decimal? balance = null, string? capTarget = null) =>
        new(id, scope, CardLayout.Note, scale, period, state, Freshness.Fresh(), [],
            new LimitFigures(used, null, null, null, limit ?? LimitValue.Unknown, null, null, balance, null), null, reset, null, capTarget, action, null);

    public static LimitCardModel PeriodUnknownCard(string id, string scope, decimal used, DateTimeOffset reset, Freshness? freshness = null) =>
        new(id, scope, CardLayout.UsedOnly, ScaleModel.Percent, PeriodModel.Unknown, CardState.PeriodUnknown, freshness ?? Freshness.Fresh(), [],
            LimitFigures.UsedOnly(used), null, Reset(reset), null, null, CardAction.None, null);

    private static AccountModel Account(string id, ProviderKind provider, string name, params LimitCardModel[] cards) =>
        new(id, provider, name, AccountHealth.Ok, null, null, null, cards);

    private static IReadOnlyList<ProviderOption> Providers(IEnumerable<AccountModel> accounts) =>
        [.. Enum.GetValues<ProviderKind>().Select(p => new ProviderOption(p, accounts.Any(a => a.Provider == p && a.Health != AccountHealth.SignedOut), false))];

    private static SettingsSummaries Summaries(IEnumerable<AccountModel> accounts, DateTimeOffset now)
    {
        var failed = accounts.Where(a => a.Health is AccountHealth.SyncFailedFresh or AccountHealth.SyncFailedStale or AccountHealth.ProviderError).Select(a => a.Provider).ToArray();
        return new SettingsSummaries(TimeSpan.FromMinutes(5), new UpdateStatus(UpdateState.UpToDate, "2026.10.604.0", now), failed.Length, failed);
    }

    public static LedgerSnapshot Snapshot(DateTimeOffset now, DayModel day, IReadOnlyList<AccountModel> accounts, IReadOnlyList<CapSettingModel>? caps = null) =>
        new(now, day, accounts, Providers(accounts), null, new BudgetSettingsModel(BudgetSettingsModel.MondayToFriday, caps ?? CapsOf(accounts)), Summaries(accounts, now));

    public static LedgerSnapshot Empty(DateTimeOffset now) => Snapshot(now, new DayModel(DayKind.WorkDay, false, null), []);

    public static IReadOnlyList<CapSettingModel> CapsOf(IEnumerable<AccountModel> accounts) =>
        [.. accounts.SelectMany(a => a.Cards.Where(c => c.Cap is not null && c.CapTargetId is not null).Select(c => new CapSettingModel(
            "cap-" + c.CardId, c.CapTargetId, a.DisplayName, c.ScopeLabel, c.Scale, c.Cap!.Amount, c.Cap.Status, c.Cap.Binding,
            c.Figures.ProviderLimit, c.Scale.Currency, c.Figures.Tracking)))];

    // ---- S1/S2: the design-brief section 4 scenario as the reference draws it ----

    public static LimitCardModel BriefClaudeWeek() =>
        FiveHour("claude-week", CardState.OnTrack, 38, 47, 56.4m, 72, At(10, 14, 16, 5), At(10, 19, 9, 0));

    public static LimitCardModel BriefClaudeExtra() =>
        Money("claude-extra", CardState.OverToday, 209, 218, 216, 300, 500, 7);

    public static LimitCardModel BriefCodexWeek() =>
        Week("codex-week", CardState.UsedUp, 96, 100, 97.7m, 20, At(10, 16, 9, 30), fiveHour: new FiveHourModel(null, 91, true, At(10, 14, 15, 48), null, null));

    public static LimitCardModel BriefCodexCredits() =>
        Credits("codex-credits", CardState.OnTrack, 6050, 6480, 6892, 17000, 842, 10160, new DateOnly(2026, 10, 3));

    private static IReadOnlyList<AccountModel> BriefAccounts() =>
    [
        Account("acct-claude", ProviderKind.Claude, "Claude Pro", BriefClaudeWeek(), BriefClaudeExtra()),
        Account("acct-codex", ProviderKind.Codex, "Codex Pro", BriefCodexWeek(), BriefCodexCredits()),
        Account("acct-copilot", ProviderKind.Copilot, "Copilot Free",
            Requests("copilot-completions", "completions", CardState.OnTrack, 1190, 1210, 1252, 2000, 62, 790),
            Requests("copilot-chat", "chat", CardState.OnTrack, 11, 12, 14, 50, 3, 38),
            Note("copilot-premium", "premium", CardState.NotIncluded, ScaleModel.Count("requests"), PeriodModel.Month(true), CopilotReset, 0, limit: LimitValue.Zero)),
        new("acct-antigravity", ProviderKind.Antigravity, "Antigravity AI Plus", AccountHealth.SyncFailedStale, At(10, 14, 13, 38), At(10, 14, 14, 15), At(10, 14, 14, 25),
        [
            Week("antigravity-g1", CardState.OnTrack, 30, 36, 53.3m, 20, At(10, 17, 11, 0), "group 1", Freshness.Stale(At(10, 14, 13, 38))),
            PeriodUnknownCard("antigravity-g2", "group 2", 19, At(10, 19, 4, 0), Freshness.Stale(At(10, 14, 13, 38))),
        ]),
    ];

    /// <summary>S5 also lists an unmatched cap and a currency mismatch; they belong to no shown card.</summary>
    private static IReadOnlyList<CapSettingModel> BriefCaps(IReadOnlyList<AccountModel> accounts) =>
    [
        .. CapsOf(accounts),
        new("cap-unmatched", null, "Copilot Business", "premium", ScaleModel.Count("requests"), 500, CapStatus.Unmatched, false, LimitValue.Unknown, null, null),
        new("cap-mismatch", "claude-team-extra", "Claude Team", "extra usage", ScaleModel.Money("EUR", 2), 250, CapStatus.CurrencyMismatch, false, LimitValue.Known(500), "USD", null),
    ];

    private static LedgerSnapshot BriefSnapshot()
    {
        var accounts = BriefAccounts();
        return Snapshot(BriefNow, new DayModel(DayKind.WorkDay, false, null), accounts, BriefCaps(accounts));
    }

    // ---- S12/S13: the same scenario on a day off, Sat 17 Oct 11:20 ----

    private static LedgerSnapshot DayOffSnapshot(bool workToday)
    {
        var now = At(10, 17, 11, 20);
        LimitCardModel Off(LimitCardModel card)
        {
            if (card.State is CardState.UsedUp or CardState.CapReached or CardState.OverCap || card.Layout is CardLayout.Note or CardLayout.UsedOnly)
                return card;
            return workToday
                ? card with { State = CardState.OnTrack, Marks = [.. card.Marks, new CardMark(MarkKind.ExtraDay)] }
                : card with { State = CardState.DayOff };
        }
        var claudeWeek = FiveHour("claude-week", CardState.OnTrack, 47, 50, 60, 25, At(10, 17, 15, 5), At(10, 19, 9, 0));
        var claudeExtra = Money("claude-extra", CardState.OnTrack, 218, 219.5m, 226, 300, 500, 7);
        var codexWeek = Week("codex-week", CardState.UsedUp, 99, 100, 100, 0, At(10, 19, 9, 30), fiveHour: new FiveHourModel(null, 0, false, null, null, null));
        var codexCredits = Credits("codex-credits", CardState.OnTrack, 6480, 6480, 6900, 17000, 842, 10160, new DateOnly(2026, 10, 3));
        var accounts = new[]
        {
            Account("acct-claude", ProviderKind.Claude, "Claude Pro", Off(claudeWeek), Off(claudeExtra)),
            Account("acct-codex", ProviderKind.Codex, "Codex Pro", Off(codexWeek), Off(codexCredits)),
            Account("acct-copilot", ProviderKind.Copilot, "Copilot Free",
                Off(Requests("copilot-completions", "completions", CardState.OnTrack, 1210, 1214, 1262, 2000, 62, 786)),
                Off(Requests("copilot-chat", "chat", CardState.OnTrack, 12, 12, 15, 50, 3, 38)),
                Note("copilot-premium", "premium", CardState.NotIncluded, ScaleModel.Count("requests"), PeriodModel.Month(true), CopilotReset, 0, limit: LimitValue.Zero)),
            Account("acct-antigravity", ProviderKind.Antigravity, "Antigravity AI Plus",
                Off(Week("antigravity-g1", CardState.OnTrack, 36, 36, 52, 20, At(10, 17, 11, 0).AddDays(7), "group 1")),
                PeriodUnknownCard("antigravity-g2", "group 2", 19, At(10, 19, 4, 0))),
        };
        return Snapshot(now, new DayModel(DayKind.DayOff, workToday, workToday ? At(10, 18, 0, 0) : null), accounts, BriefCaps(accounts));
    }

    // ---- S10b: Fri 30 Oct 16:10, the last work day before several resets ----

    private static LedgerSnapshot LastWorkDaySnapshot()
    {
        var now = At(10, 30, 16, 10);
        var expired = Freshness.Stale(At(10, 30, 15, 2));
        var signIn = new[] { new CardMark(MarkKind.SignInExpired) };
        var accounts = new AccountModel[]
        {
            Account("acct-claude", ProviderKind.Claude, "Claude Pro",
                FiveHour("claude-week", CardState.FiveHourFull, 40, 46, 58, 100, At(10, 30, 18, 5), At(11, 2, 9, 0),
                    marks: [new CardMark(MarkKind.OnExtraUsage, At(10, 30, 13, 5), At(10, 30, 18, 5), 1.40m, "USD", 2)]),
                Money("claude-extra", CardState.OnTrack, 209, 211, 216, 300, 500, 7)),
            Account("acct-codex", ProviderKind.Codex, "Codex Pro",
                Week("codex-week", CardState.Rush, 62, 66, 100, 20, At(10, 31, 9, 30), fiveHour: new FiveHourModel(null, 20, true, At(10, 30, 19, 40), null, null)),
                Credits("codex-credits", CardState.OnTrack, 16700, 16760, 17000, 17000, 842, 3400, new DateOnly(2026, 10, 3))),
            Account("acct-copilot", ProviderKind.Copilot, "Copilot Free",
                Requests("copilot-completions", "completions", CardState.Rush, 1800, 1850, 2000, 2000, 62, 150),
                Requests("copilot-chat", "chat", CardState.OnTrack, 37, 38, 40, 50, 5, 12, cap: 40)),
            new("acct-antigravity", ProviderKind.Antigravity, "Antigravity AI Plus", AccountHealth.SignInExpired, At(10, 30, 15, 2), null, null,
            [
                Week("antigravity-g1", CardState.OnTrack, 30, 36, 53.3m, 20, At(10, 31, 11, 0), "group 1", expired, signIn) with { Action = CardAction.SignIn },
                PeriodUnknownCard("antigravity-g2", "group 2", 19, At(11, 2, 4, 0), expired) with { Marks = signIn },
            ]),
        };
        return Snapshot(now, new DayModel(DayKind.WorkDay, false, null), accounts);
    }

    // ---- S9: a cancelled sign-in and an expired one ----

    private static LedgerSnapshot SignInSnapshot()
    {
        var expired = Freshness.Stale(At(10, 14, 13, 20));
        var marks = new[] { new CardMark(MarkKind.SignInExpired) };
        var claude = new AccountModel("acct-claude", ProviderKind.Claude, "Claude Pro", AccountHealth.SignInExpired, At(10, 14, 13, 20), null, null,
        [
            BriefClaudeWeek() with { Freshness = expired, Marks = marks, Action = CardAction.SignIn },
            BriefClaudeExtra() with { Freshness = expired, Marks = marks },
        ]);
        var snapshot = Snapshot(BriefNow, new DayModel(DayKind.WorkDay, false, null), [claude]);
        return snapshot with { SignInStrip = new SignInStripModel(SignInPhase.Cancelled, ProviderKind.Codex, null, null) };
    }

    /// <summary>S8: the account a successful demo sign-in adds.</summary>
    public static AccountModel SignedInAccount(ProviderKind provider) => provider switch
    {
        ProviderKind.Antigravity => Account("acct-antigravity", provider, "Antigravity AI Plus",
            Week("antigravity-g1", CardState.OnTrack, 30, 36, 53.3m, 20, At(10, 17, 11, 0), "group 1"),
            PeriodUnknownCard("antigravity-g2", "group 2", 19, At(10, 19, 4, 0))),
        _ => BriefAccounts().First(a => a.Provider == provider) with { Health = AccountHealth.Ok, LastSyncFailedAt = null, NextRetryAt = null },
    };

    // ---- Provider States: every card state, one account per reference case ----

    private static LedgerSnapshot StatesSnapshot()
    {
        var thu9 = At(10, 15, 9, 0);
        var mon9 = At(10, 19, 9, 0);
        var sat11 = At(10, 17, 11, 0);
        var win = At(10, 14, 17, 35);
        LimitCardModel W5(string id, CardState state, decimal u0, decimal u, decimal t, decimal cwU, DateTimeOffset? end = null, DateTimeOffset? reset = null,
            IReadOnlyList<CardMark>? marks = null, Freshness? freshness = null, bool started = true, int? fit = null, CardAction action = CardAction.None, DayOffPreview? dayOff = null) =>
            FiveHour(id, state, u0, u, t, cwU, end ?? win, reset ?? mon9, started: started, fit: fit, marks: marks, freshness: freshness, action: action, dayOff: dayOff);
        LimitCardModel D7(string id, CardState state, decimal u0, decimal u, decimal t, decimal usual = 14, DateTimeOffset? reset = null, string? scope = null,
            IReadOnlyList<CardMark>? marks = null, DayOffPreview? dayOff = null) =>
            Week(id, state, u0, u, t, usual, reset ?? sat11, scope, marks: marks, dayOff: dayOff);
        LimitCardModel Cr(string id, CardState state, decimal u0, decimal u, decimal t) =>
            Credits(id, state, u0, u, t, 15000, 500, 20000 - u, new DateOnly(2026, 10, 3));
        LimitCardModel Us(string id, CardState state, decimal u0, decimal u, decimal t) =>
            Money(id, state, u0, u, t, 300, 500, 10, "$500 · month");
        var extraDay = new[] { new CardMark(MarkKind.ExtraDay) };

        var cases = new List<(string Case, ProviderKind Provider, string Name, LimitCardModel Card, AccountHealth Health)>
        {
            ("A1", ProviderKind.Claude, "Claude Max", W5("a1", CardState.OnTrack, 44, 50, 82.4m, 30, reset: thu9), AccountHealth.Ok),
            ("A2", ProviderKind.Claude, "Claude Pro", W5("a2", CardState.TodayLow, 40, 47, 48, 40), AccountHealth.Ok),
            ("A3", ProviderKind.Claude, "Claude Team", W5("a3", CardState.OverToday, 32, 43, 42, 60), AccountHealth.Ok),
            ("A4", ProviderKind.Claude, "Claude Max", W5("a4", CardState.FiveHourFull, 30, 36, 54, 100, At(10, 14, 16, 20)), AccountHealth.Ok),
            ("A5", ProviderKind.Claude, "Claude Max", W5("a5", CardState.OnTrack, 20, 20, 50, 0, started: false), AccountHealth.Ok),
            ("A6", ProviderKind.Claude, "Claude Pro", W5("a6", CardState.UsedUp, 96, 100, 100, 35), AccountHealth.Ok),
            ("A7", ProviderKind.Claude, "Claude Max", W5("a7", CardState.OnTrack, 44, 50, 82.4m, 30, freshness: Freshness.Stale(At(10, 14, 12, 18))), AccountHealth.SyncFailedStale),
            ("A8", ProviderKind.Claude, "Claude Pro", W5("a8", CardState.FiveHourFull, 30, 36, 54, 100, At(10, 14, 16, 5),
                marks: [new CardMark(MarkKind.OnExtraUsage, At(10, 14, 13, 5), At(10, 14, 16, 5), 2.00m, "USD", 2)]), AccountHealth.Ok),
            ("B1", ProviderKind.Antigravity, "Antigravity AI Plus", D7("b1", CardState.OnTrack, 0, 0, 14, reset: At(10, 21, 14, 0)), AccountHealth.Ok),
            ("B2", ProviderKind.Antigravity, "Antigravity AI Plus", D7("b2", CardState.OnTrack, 30, 34, 44), AccountHealth.Ok),
            ("B3", ProviderKind.Antigravity, "Antigravity AI Plus", D7("b3", CardState.TodayUsed, 30, 44, 44), AccountHealth.Ok),
            ("B4", ProviderKind.Antigravity, "Antigravity AI Plus", D7("b4", CardState.OverToday, 30, 47, 44), AccountHealth.Ok),
            ("B5", ProviderKind.Antigravity, "Antigravity AI Plus", D7("b5", CardState.TodayShort, 92, 93, 100), AccountHealth.Ok),
            ("C1", ProviderKind.Codex, "Codex", Cr("c1", CardState.OnTrack, 6000, 6150, 6500), AccountHealth.Ok),
            ("C2", ProviderKind.Codex, "Codex", Cr("c2", CardState.TodayUsed, 6000, 6500, 6500), AccountHealth.Ok),
            ("C3", ProviderKind.Codex, "Codex", Cr("c3", CardState.OverToday, 6000, 6740, 6500), AccountHealth.Ok),
            ("C4", ProviderKind.Codex, "Codex", Cr("c4", CardState.CapClose, 14600, 14700, 15000), AccountHealth.Ok),
            ("C5", ProviderKind.Codex, "Codex", Cr("c5", CardState.CapReached, 14800, 15000, 15000), AccountHealth.Ok),
            ("C6", ProviderKind.Codex, "Codex", Cr("c6", CardState.OverCap, 15000, 15400, 15000), AccountHealth.Ok),
            ("D1", ProviderKind.Claude, "Claude Work", Us("d1", CardState.OnTrack, 120, 124.5m, 130), AccountHealth.Ok),
            ("D2", ProviderKind.Claude, "Claude Work", Us("d2", CardState.TodayUsed, 120, 130, 130), AccountHealth.Ok),
            ("D3", ProviderKind.Claude, "Claude Work", Us("d3", CardState.OverToday, 120, 138.4m, 130), AccountHealth.Ok),
            ("D4", ProviderKind.Claude, "Claude Work", Us("d4", CardState.CapClose, 292, 294, 300), AccountHealth.Ok),
            ("D5", ProviderKind.Claude, "Claude Work", Us("d5", CardState.CapReached, 296, 300, 300), AccountHealth.Ok),
            ("D6", ProviderKind.Claude, "Claude Work", Us("d6", CardState.OverCap, 300, 312.6m, 300), AccountHealth.Ok),
            ("G1", ProviderKind.Copilot, "Copilot Free", Requests("g1", "completions", CardState.OnTrack, 1190, 1210, 1252, 2000, 62, 790), AccountHealth.Ok),
            ("G2", ProviderKind.Copilot, "Copilot Free", Requests("g2", "chat", CardState.TodayUsed, 11, 14, 14, 50, 3, 36), AccountHealth.Ok),
            ("G3", ProviderKind.Copilot, "Copilot Pro", Requests("g3", "chat · unlimited", CardState.OnTrack, 120, 126, 134, 300, 14, null, cap: 300, unlimited: true), AccountHealth.Ok),
            ("G4", ProviderKind.Copilot, "Copilot Free", Note("g4", "premium", CardState.NotIncluded, ScaleModel.Count("requests"), PeriodModel.Month(true), CopilotReset, 0, limit: LimitValue.Zero), AccountHealth.Ok),
            ("G5", ProviderKind.Copilot, "Copilot Business", Note("g5", "premium", CardState.LimitUnknown, ScaleModel.Count("requests"), PeriodModel.Month(true), CopilotReset, 340, CardAction.SetCap, capTarget: "g5"), AccountHealth.Ok),
            ("H1", ProviderKind.Antigravity, "Antigravity AI Plus", PeriodUnknownCard("h1", "group 2", 19, At(10, 19, 4, 0)), AccountHealth.Ok),
            ("H2", ProviderKind.Codex, "Codex Pro", Week("h2", CardState.OnTrack, 30, 33, 48, 18, At(10, 16, 9, 30), fiveHour: new FiveHourModel(null, 40, true, At(10, 14, 17, 10), null, null)), AccountHealth.Ok),
            ("H3", ProviderKind.Claude, "Claude Max", FiveHour("h3", CardState.OnTrack, 44, 50, 82.4m, 30, win, mon9, ws: 10, low: 7, high: 13, rough: true), AccountHealth.Ok),
            ("H5", ProviderKind.Claude, "Claude Pro", new LimitCardModel("h5", null, CardLayout.UsedOnly, ScaleModel.Percent, PeriodModel.Week, CardState.NotReady,
                Freshness.Stale(At(10, 14, 13, 55)), [new CardMark(MarkKind.PastReset)], LimitFigures.UsedOnly(88), null, Reset(At(10, 14, 14, 0)), null, null, CardAction.None, null), AccountHealth.Ok),
            ("H6", ProviderKind.Antigravity, "Antigravity AI Plus", Note("h6", null, CardState.ValueUnknown, ScaleModel.Percent, PeriodModel.Week, Reset(sat11)), AccountHealth.Ok),
            ("H7", ProviderKind.Claude, "Claude Max", W5("h7", CardState.OnTrack, 44, 50, 82.4m, 30, marks: [new CardMark(MarkKind.SyncFailed)], freshness: Freshness.Fresh(At(10, 14, 14, 10))), AccountHealth.SyncFailedFresh),
            ("H8", ProviderKind.Claude, "Claude Pro", W5("h8", CardState.OnTrack, 40, 47, 60, 40, marks: [new CardMark(MarkKind.SignInExpired)], freshness: Freshness.Stale(At(10, 14, 13, 20)), action: CardAction.SignIn), AccountHealth.SignInExpired),
            ("H9", ProviderKind.Codex, "Codex Pro", Note("h9", null, CardState.SignedOut, ScaleModel.Percent, PeriodModel.Week, null, action: CardAction.SignIn), AccountHealth.SignedOut),
            ("H11", ProviderKind.Claude, "Claude", Note("h11", null, CardState.NoDisplayedLimits, ScaleModel.Percent, PeriodModel.Unknown, null), AccountHealth.Ok),
            ("H10", ProviderKind.Claude, "Claude Max", D7("h10", CardState.OnTrack, 20, 23, 34, reset: mon9, scope: "7d · Opus"), AccountHealth.Ok),
            ("O1", ProviderKind.Claude, "Claude Max", W5("o1", CardState.DayOff, 50, 56, 62.5m, 50, dayOff: new DayOffPreview(DayOfWeek.Monday, 16.7m, 14.7m)), AccountHealth.Ok),
            ("O2", ProviderKind.Antigravity, "Antigravity AI Plus", D7("o2", CardState.DayOff, 40, 58, 55, dayOff: new DayOffPreview(DayOfWeek.Monday, 20.0m, 14.0m)), AccountHealth.Ok),
            ("O3", ProviderKind.Antigravity, "Antigravity AI Plus", D7("o3", CardState.DayOff, 62, 62, 100, 0), AccountHealth.Ok),
            ("O4", ProviderKind.Codex, "Codex Pro", Week("o4", CardState.UsedUp, 97, 100, 100, 0, At(10, 19, 9, 30), fiveHour: new FiveHourModel(null, 0, false, null, null, null)), AccountHealth.Ok),
            ("O5", ProviderKind.Claude, "Claude Max", W5("o5", CardState.OnTrack, 50, 56, 62.5m, 50, marks: extraDay), AccountHealth.Ok),
            ("O6", ProviderKind.Antigravity, "Antigravity AI Plus", D7("o6", CardState.OverToday, 40, 58, 55, marks: extraDay), AccountHealth.Ok),
            ("R1", ProviderKind.Antigravity, "Antigravity AI Plus", D7("r1", CardState.Rush, 62, 70, 100, reset: thu9), AccountHealth.Ok),
            ("R2", ProviderKind.Claude, "Claude Pro", W5("r2", CardState.Rush, 61, 67, 100, 50, reset: thu9), AccountHealth.Ok),
            ("R3", ProviderKind.Claude, "Claude Max", W5("r3", CardState.Rush, 40, 40, 100, 0, reset: At(10, 14, 21, 0), started: false, fit: 2), AccountHealth.Ok),
            ("R4", ProviderKind.Antigravity, "Antigravity AI Plus", D7("r4", CardState.UsedUp, 62, 100, 100, reset: thu9), AccountHealth.Ok),
            ("R5", ProviderKind.Copilot, "Copilot Free", Requests("r5", "completions", CardState.Rush, 1800, 1850, 2000, 2000, 62, 150), AccountHealth.Ok),
            ("R6", ProviderKind.Copilot, "Copilot Free", Requests("r6", "chat", CardState.OnTrack, 37, 38, 40, 50, 5, 12, cap: 40), AccountHealth.Ok),
            ("R7", ProviderKind.Codex, "Codex", Cr("r7", CardState.OnTrack, 14700, 14760, 15000), AccountHealth.Ok),
            ("R8", ProviderKind.Claude, "Claude Work", Us("r8", CardState.OnTrack, 280, 284, 300), AccountHealth.Ok),
        };
        var accounts = cases.Select(c => new AccountModel("acct-" + c.Case.ToLowerInvariant(), c.Provider, c.Case + " · " + c.Name, c.Health,
            c.Card.Freshness.ReadingAt, c.Health is AccountHealth.SyncFailedFresh or AccountHealth.SyncFailedStale ? At(10, 14, 14, 18) : null,
            c.Health is AccountHealth.SyncFailedFresh or AccountHealth.SyncFailedStale ? At(10, 14, 14, 25) : null, [c.Card])).ToArray();
        return Snapshot(BriefNow, new DayModel(DayKind.WorkDay, false, null), accounts);
    }

    /// <summary>36 local days of synthetic use ending today, with a four-day gap and weekly resets on Monday.</summary>
    public static HistoryModel History(LimitCardModel card, DateTimeOffset now)
    {
        int[] pattern = [14, 18, 16, 21, 12, 3, 0];
        int[] jitter = [2, -3, 1, 0, -2, 4, -1, 3, -4, 2, 0, 1, -2, 3, -1, 0, 2, -3, 1, 4, -2, 0, 3, -1, 2, -4, 1, 0, -2, 3, 1, -1, 2, 0, -3, 0];
        var today = DateOnly.FromDateTime(now.DateTime);
        var scale = card.Scale.Kind == ScaleKind.Percent ? 1m : (card.Figures.UsualShare ?? 20) / 20m;
        var days = new List<HistoryDay>(36);
        var resets = new List<DateOnly>();
        for (var i = 0; i < 36; i++)
        {
            var date = today.AddDays(i - 35);
            var weekday = ((int)date.DayOfWeek + 6) % 7;
            if (weekday == 0)
                resets.Add(date);
            if (i is >= 16 and <= 19)
            {
                days.Add(new HistoryDay(date, null));
                continue;
            }
            var value = Math.Max(0, pattern[weekday] + (weekday < 5 ? jitter[i] : Math.Min(0, jitter[i])));
            if (i == 35 && card.Figures is { Used: { } u, DayStart: { } u0 })
                days.Add(new HistoryDay(date, u - u0));
            else
                days.Add(new HistoryDay(date, value * scale));
        }
        return new HistoryModel(card.CardId, card.Period, days, card.Scale.Kind == ScaleKind.Percent ? 20 : card.Figures.UsualShare, resets, today);
    }
}

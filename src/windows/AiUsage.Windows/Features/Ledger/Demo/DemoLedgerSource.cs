using AiUsage.Features.Ledger.Contract;

namespace AiUsage.Features.Ledger.Demo;

/// <summary>
/// In-memory ILedgerSource over the synthetic scenarios. Commands change the snapshot the way a live source would publish
/// the result; cap edits only replace the cap and effective limit (the demo has no budget engine), and sign-in completes
/// after a short delay. Nothing is read from or written to disk.
/// </summary>
internal sealed class DemoLedgerSource(ILedgerScheduler scheduler) : ILedgerSource
{
    private readonly Dictionary<string, LimitCardModel> originals = [];
    private readonly Dictionary<string, LimitCardModel> natives = [];
    private readonly Dictionary<string, (decimal? DayStart, decimal? TodayEnd)> todayBases = [];
    private readonly Dictionary<string, IReadOnlyList<LimitCardModel>> signedOutCards = [];
    private IDisposable? pendingSignIn;
    private bool workToday;

    public LedgerSnapshot Current { get; private set; } = DemoLedgerScenarios.Build(DemoLedgerScenarios.Brief);
    public LedgerPreferences Preferences { get; private set; } = LedgerPreferences.Default;
    public string ScenarioId { get; private set; } = DemoLedgerScenarios.Brief;
    public event EventHandler? Changed;

    public void LoadScenario(string id)
    {
        pendingSignIn?.Dispose();
        pendingSignIn = null;
        workToday = false;
        ScenarioId = id;
        originals.Clear();
        natives.Clear();
        todayBases.Clear();
        signedOutCards.Clear();
        Publish(DemoLedgerScenarios.Build(id));
    }

    private void Publish(LedgerSnapshot snapshot)
    {
        // T-055 R-14: the settings summary shows the chosen refresh interval in every scenario.
        Current = snapshot with { Summaries = snapshot.Summaries with { RefreshInterval = TimeSpan.FromMinutes(Preferences.RefreshMinutes) } };
        foreach (var card in snapshot.Accounts.SelectMany(a => a.Cards).Where(c => c.CapTargetId is not null))
            originals.TryAdd(card.CapTargetId!, card);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task RefreshAsync(CancellationToken ct)
    {
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task SetWorkTodayAsync(bool on, CancellationToken ct)
    {
        if (Current.Day.Kind != DayKind.DayOff || workToday == on)
            return Task.CompletedTask;
        workToday = on;
        var baseline = DemoLedgerScenarios.Build(ScenarioId, !on).Accounts.SelectMany(a => a.Cards).ToDictionary(c => c.CardId);
        var changed = DemoLedgerScenarios.Build(ScenarioId, on).Accounts.SelectMany(a => a.Cards).ToDictionary(c => c.CardId);
        // Only switch the day's presentation. Never resurrect a signed-out account or reset edits/order.
        LimitCardModel SwitchDay(LimitCardModel card)
        {
            if (!baseline.TryGetValue(card.CardId, out var before) || !changed.TryGetValue(card.CardId, out var after))
                return card;
            return card with
            {
                State = card.State == before.State ? after.State : card.State,
                Marks = [.. card.Marks.Where(m => m.Kind != MarkKind.ExtraDay), .. after.Marks.Where(m => m.Kind == MarkKind.ExtraDay)],
            };
        }
        foreach (var id in signedOutCards.Keys.ToArray())
            signedOutCards[id] = [.. signedOutCards[id].Select(SwitchDay)];
        Publish(Current with
        {
            Day = DemoLedgerScenarios.Build(ScenarioId, on).Day,
            Accounts = [.. Current.Accounts.Select(a => a.Health == AccountHealth.SignedOut ? a : a with { Cards = [.. a.Cards.Select(SwitchDay)] })],
        });
        return Task.CompletedTask;
    }

    public Task<CommandOutcome> RenameAccountAsync(string accountId, string name, CancellationToken ct)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is 0 or > 64 || Current.Accounts.All(a => a.AccountId != accountId))
            return Task.FromResult(CommandOutcome.Rejected);
        Publish(Current with { Accounts = [.. Current.Accounts.Select(a => a.AccountId == accountId ? a with { DisplayName = trimmed } : a)] });
        return Task.FromResult(CommandOutcome.Done);
    }

    public Task<CommandOutcome> SetCapAsync(string capTargetId, decimal? amount, CancellationToken ct)
    {
        if (amount is < 0)
            return Task.FromResult(CommandOutcome.Rejected);
        var account = Current.Accounts.FirstOrDefault(a => a.Cards.Any(c => c.CapTargetId == capTargetId));
        if (account is null)
            return Task.FromResult(CommandOutcome.Rejected);
        var card = account.Cards.First(c => c.CapTargetId == capTargetId);
        if (amount is { } requested && !card.Figures.ProviderLimit.AllowsCap(requested))
            return Task.FromResult(CommandOutcome.Rejected);
        // R-199: a credit pool keeps its cap in credits; one entered in dollars is converted first.
        var native = natives.GetValueOrDefault(card.CardId) ?? (card.Units is not null ? card : null);
        var original = native ?? originals.GetValueOrDefault(capTargetId, card);
        var credits = native?.Units is { Usd: true, Rate: var rate } && amount is { } dollars ? CreditDollars.CapCredits(dollars, rate) : amount;
        var updated = credits is { } value ? WithCap(original, native ?? card, value) : WithoutCap(original, native ?? card);
        if (native is not null)
        {
            natives[card.CardId] = updated;
            updated = CreditDollars.Apply(updated, null);
        }
        if (updated.Monetary is { } money && updated.Scale.Exponent is >= 0 and <= 18)
        {
            decimal scale = 1;
            for (int i = 0; i < updated.Scale.Exponent; i++) scale *= 10;
            updated = updated with { Monetary = money with { PersonalCap = amount is { } capAmount
                ? new(checked((long)(capAmount * scale)), updated.Scale.Exponent, updated.Scale.Currency) : null } };
        }
        var accounts = Current.Accounts.Select(a => a.AccountId != account.AccountId ? a : a with { Cards = [.. a.Cards.Select(c => c.CardId == card.CardId ? updated : c)] }).ToArray();
        var caps = Current.Budget.Caps.ToList();
        var index = caps.FindIndex(c => c.CapTargetId == capTargetId);
        if (index >= 0)
            caps.RemoveAt(index);
        else
            index = caps.Count;
        if (updated.Cap is { } cap)
            caps.Insert(index, new CapSettingModel("cap-" + updated.CardId, capTargetId, account.DisplayName, updated.ScopeLabel, updated.Scale, cap.Amount, cap.Status, cap.Binding,
                    updated.Figures.ProviderLimit, updated.Scale.Currency, updated.Figures.Tracking));
        Publish(Current with { Accounts = accounts, Budget = Current.Budget with { Caps = caps } });
        return Task.FromResult(CommandOutcome.Done);
    }

    private static LimitCardModel WithCap(LimitCardModel original, LimitCardModel current, decimal amount)
    {
        var provider = original.Figures.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit } ? limit : (decimal?)null;
        var binding = provider is null || amount <= provider;
        if (original.Layout != CardLayout.Note)
            return original with
            {
                Cap = new CapModel(amount, binding, CapStatus.Applied),
                Figures = original.Figures with { EffectiveLimit = provider is { } p ? Math.Min(p, amount) : amount },
                Action = CardAction.None,
            };
        // A pool that had no cap gains a demo budget on the cap; a live source would publish engine figures here.
        var used = current.Figures.Used ?? 0;
        return original with
        {
            Layout = CardLayout.Pool,
            State = CardState.OnTrack,
            Cap = new CapModel(amount, binding, CapStatus.Applied),
            Action = CardAction.None,
            Figures = original.Figures with { DayStart = used, TodayEnd = used + Math.Floor(Math.Max(0, amount - used) / 13), UsualShare = Math.Floor(amount / 22), EffectiveLimit = amount, Used = used },
        };
    }

    private static LimitCardModel WithoutCap(LimitCardModel original, LimitCardModel current)
    {
        if (original.Figures.ProviderLimit is { Kind: LimitValueKind.Known, Amount: { } limit })
            return current with { Cap = null, Figures = current.Figures with { EffectiveLimit = limit } };
        return current with
        {
            Layout = CardLayout.Note,
            State = original.State == CardState.LimitUnknown ? CardState.LimitUnknown : CardState.NoCap,
            Cap = null,
            Action = CardAction.None,
            Figures = current.Figures with { DayStart = null, TodayEnd = null, EffectiveLimit = null },
        };
    }

    public Task<CommandOutcome> RemoveUnmatchedCapAsync(string capId, CancellationToken ct)
    {
        if (Current.Budget.Caps.All(c => c.CapId != capId || c.Status != CapStatus.Unmatched))
            return Task.FromResult(CommandOutcome.Rejected);
        Publish(Current with { Budget = Current.Budget with { Caps = [.. Current.Budget.Caps.Where(c => c.CapId != capId)] } });
        return Task.FromResult(CommandOutcome.Done);
    }

    public Task<CommandOutcome> SetTodayUsedAsync(string cardId, decimal? amount, CancellationToken ct)
    {
        var card = Current.Accounts.SelectMany(a => a.Cards).FirstOrDefault(c => c.CardId == cardId);
        if (card?.TodayUse is null || card.Figures.Used is not { } shown || amount < 0 || amount > shown ||
            amount is { } typed && card.Scale.Kind == ScaleKind.Money && decimal.Round(typed, card.Scale.Exponent ?? 2) != typed)
            return Task.FromResult(CommandOutcome.Rejected);
        var native = natives.GetValueOrDefault(cardId, card);
        if (!todayBases.TryGetValue(cardId, out var day))
            todayBases[cardId] = day = (native.Figures.DayStart, native.Figures.TodayEnd);
        var used = native.Figures.Used ?? 0;
        // The demo has no budget engine: today's share keeps its width and moves with the day start.
        var start = amount is { } value ? used - (native.Units is { Usd: true, Rate: var rate } ? Math.Min(CreditDollars.TodayCredits(value, rate), used) : value) : day.DayStart;
        var updated = native with
        {
            Figures = native.Figures with { DayStart = start, TodayEnd = start + (day.TodayEnd - day.DayStart) },
            TodayUse = native.TodayUse! with { Manual = amount is not null },
        };
        Replace(updated);
        return Task.FromResult(CommandOutcome.Done);
    }

    public Task<CommandOutcome> SetUnitsAsync(string cardId, bool usd, decimal rate, CancellationToken ct)
    {
        var card = Current.Accounts.SelectMany(a => a.Cards).FirstOrDefault(c => c.CardId == cardId);
        if (card?.Units is null || !CreditDollars.ValidRate(rate))
            return Task.FromResult(CommandOutcome.Rejected);
        Replace(natives.GetValueOrDefault(cardId, card) with { Units = new(usd, rate) });
        return Task.FromResult(CommandOutcome.Done);
    }

    /// <summary>Publishes a card from its native figures; a credit pool keeps them and shows its chosen unit (R-199).</summary>
    private void Replace(LimitCardModel native)
    {
        if (native.Units is not null)
            natives[native.CardId] = native;
        var shown = CreditDollars.Apply(native, null);
        Publish(Current with
        {
            Accounts = [.. Current.Accounts.Select(a => a with { Cards = [.. a.Cards.Select(c => c.CardId == shown.CardId ? shown : c)] })],
            Budget = Current.Budget with
            {
                Caps = [.. Current.Budget.Caps.Select(c => c.CapTargetId == shown.CapTargetId && shown.Cap is { } cap ? c with { Scale = shown.Scale, Amount = cap.Amount, ProviderLimit = shown.Figures.ProviderLimit } : c)]
            },
        });
    }

    public Task<CommandOutcome> SetWorkDaysAsync(IReadOnlySet<DayOfWeek> days, CancellationToken ct)
    {
        if (days.Count == 0)
            return Task.FromResult(CommandOutcome.Rejected);
        Publish(Current with { Budget = Current.Budget with { WorkDays = new HashSet<DayOfWeek>(days) } });
        return Task.FromResult(CommandOutcome.Done);
    }

    public Task MoveCardAsync(string cardId, int offset, CancellationToken ct)
    {
        var accounts = Current.Accounts.Select(a =>
        {
            var index = a.Cards.ToList().FindIndex(c => c.CardId == cardId);
            var target = index + offset;
            if (index < 0 || target < 0 || target >= a.Cards.Count)
                return a;
            var cards = a.Cards.ToList();
            (cards[index], cards[target]) = (cards[target], cards[index]);
            return a with { Cards = cards };
        }).ToArray();
        Publish(Current with { Accounts = accounts });
        return Task.CompletedTask;
    }

    public Task<CommandOutcome> MoveAccountAsync(string accountId, string? beforeAccountId, CancellationToken ct)
    {
        if (AccountCard.Move([.. Current.Accounts.Select(a => a.AccountId)], accountId, beforeAccountId) is not { } order)
            return Task.FromResult(CommandOutcome.Rejected);
        Publish(Current with { Accounts = [.. Current.Accounts.OrderBy(a => Array.IndexOf(order, a.AccountId))] });
        return Task.FromResult(CommandOutcome.Done);
    }

    public Task<CommandOutcome> SetCardHiddenAsync(string cardId, bool hidden, CancellationToken ct)
    {
        var account = Current.Accounts.FirstOrDefault(a => a.Cards.Any(c => c.CardId == cardId));
        if (account is null || hidden && AccountCard.Primary(account)?.CardId == cardId)
            return Task.FromResult(CommandOutcome.Rejected);
        var changed = account with { Cards = [.. account.Cards.Select(c => c.CardId == cardId ? c with { Hidden = hidden } : c)] };
        Publish(Current with { Accounts = [.. Current.Accounts.Select(a => a.AccountId == account.AccountId ? changed : a)] });
        return Task.FromResult(CommandOutcome.Done);
    }

    public Task SignInAsync(ProviderKind provider, CancellationToken ct) => BeginSignIn(provider, null);

    public Task ReconnectAsync(string accountId, CancellationToken ct)
    {
        var account = Current.Accounts.FirstOrDefault(a => a.AccountId == accountId);
        return account is null ? Task.CompletedTask : BeginSignIn(account.Provider, accountId);
    }

    public Task RefreshAccountAsync(string accountId, CancellationToken ct) => RefreshAsync(ct);
    public bool TrySubmitSignInCode(Guid attemptId, string code)
    {
        if (Current.SignInStrip is not { Phase: SignInPhase.Waiting, AcceptsManualCode: true } strip || strip.AttemptId != attemptId || string.IsNullOrWhiteSpace(code))
            return false;
        pendingSignIn?.Dispose();
        CompleteSignIn(strip.Provider, strip.ReconnectAccountId);
        return true;
    }

    private Task BeginSignIn(ProviderKind provider, string? reconnect)
    {
        if (Current.SignInStrip?.Phase == SignInPhase.Waiting) return Task.CompletedTask;
        pendingSignIn?.Dispose();
        Publish(Current with
        {
            // Each demo provider shows its real sign-in shape: a device code, a pasted code, or the browser alone.
            SignInStrip = new SignInStripModel(SignInPhase.Waiting, provider, null, null)
            {
                AttemptId = Guid.NewGuid(), ReconnectAccountId = reconnect,
                UserCode = provider is ProviderKind.Codex or ProviderKind.Copilot ? "WDJB-MJHT" : null,
                AcceptsManualCode = provider == ProviderKind.Claude,
            },
            Providers = [.. Current.Providers.Select(p => p with { SigningIn = p.Provider == provider })],
        });
        pendingSignIn = scheduler.Schedule(TimeSpan.FromSeconds(3), () => CompleteSignIn(provider, reconnect));
        return Task.CompletedTask;
    }

    private void CompleteSignIn(ProviderKind provider, string? reconnect)
    {
        pendingSignIn = null;
        var existing = Current.Accounts.FirstOrDefault(a => a.AccountId == reconnect);
        AccountModel account;
        if (existing is not null && signedOutCards.Remove(existing.AccountId, out var cards))
            account = existing with { Health = AccountHealth.Ok, Cards = cards };
        else if (existing is not null)
            account = existing with { Health = AccountHealth.Ok, Cards = [.. existing.Cards.Select(c => c with { Freshness = Freshness.Fresh(), Marks = [.. c.Marks.Where(m => m.Kind != MarkKind.SignInExpired)], Action = c.Action == CardAction.SignIn ? CardAction.None : c.Action })] };
        else
            account = DemoLedgerScenarios.SignedInAccount(provider);
        if (existing is null && Current.Accounts.Any(a => a.AccountId == account.AccountId))
        {
            var suffix = "-" + Guid.NewGuid().ToString("N");
            account = account with { AccountId = account.AccountId + suffix, DisplayName = account.DisplayName + " 2",
                Cards = account.Cards.Select(c => c with { CardId = c.CardId + suffix, CapTargetId = c.CapTargetId is null ? null : c.CapTargetId + suffix }).ToArray() };
        }
        var accounts = existing is null ? [.. Current.Accounts, account] : Current.Accounts.Select(a => a.AccountId == existing.AccountId ? account : a).ToArray();
        Publish(Current with
        {
            Accounts = accounts,
            SignInStrip = new SignInStripModel(SignInPhase.Succeeded, provider, account.DisplayName, account.Cards.Count),
            Providers = [.. Current.Providers.Select(p => p.Provider == provider ? p with { Added = true, SigningIn = false } : p)],
        });
    }

    public Task CancelSignInAsync(CancellationToken ct)
    {
        if (Current.SignInStrip is not { Phase: SignInPhase.Waiting } strip)
            return Task.CompletedTask;
        pendingSignIn?.Dispose();
        pendingSignIn = null;
        Publish(Current with
        {
            SignInStrip = strip with { Phase = SignInPhase.Cancelled },
            Providers = [.. Current.Providers.Select(p => p with { SigningIn = false })],
        });
        return Task.CompletedTask;
    }

    public void FailSignIn()
    {
        pendingSignIn?.Dispose();
        pendingSignIn = null;
        var provider = Current.SignInStrip?.Provider ?? ProviderKind.Codex;
        Publish(Current with
        {
            SignInStrip = new SignInStripModel(SignInPhase.Failed, provider, null, null),
            Providers = [.. Current.Providers.Select(p => p with { SigningIn = false })],
        });
    }

    /// <summary>Clears a finished strip (the view model hides a success after 4 s).</summary>
    public void DismissStrip()
    {
        if (Current.SignInStrip is { Phase: not SignInPhase.Waiting })
            Publish(Current with { SignInStrip = null });
    }

    public Task SignOutAsync(string accountId, CancellationToken ct)
    {
        var account = Current.Accounts.FirstOrDefault(a => a.AccountId == accountId);
        if (account is null || account.Health == AccountHealth.SignedOut)
            return Task.CompletedTask;
        signedOutCards[accountId] = account.Cards;
        var card = DemoLedgerScenarios.Note((account.Cards.Count > 0 ? account.Cards[0].CardId : null) ?? accountId + "-signed-out", null, CardState.SignedOut,
            ScaleModel.Percent, PeriodModel.Week, null, action: CardAction.SignIn);
        var signedOut = account with { Health = AccountHealth.SignedOut, Cards = [card] };
        Publish(Current with
        {
            Accounts = [.. Current.Accounts.Select(a => a.AccountId == accountId ? signedOut : a)],
            Providers = [.. Current.Providers.Select(p => p.Provider == account.Provider ? p with
                { Added = Current.Accounts.Any(a => a.AccountId != accountId && a.Provider == account.Provider && a.Health != AccountHealth.SignedOut) } : p)],
        });
        return Task.CompletedTask;
    }

    public Task<CommandOutcome> DeleteStoredDataAsync(CancellationToken ct)
    {
        pendingSignIn?.Dispose();
        pendingSignIn = null;
        originals.Clear();
        natives.Clear();
        todayBases.Clear();
        signedOutCards.Clear();
        workToday = false;
        ScenarioId = DemoLedgerScenarios.FirstRun;
        Publish(DemoLedgerScenarios.Empty(Current.LocalNow));
        return Task.FromResult(CommandOutcome.Done);
    }

    public Task SetPreferencesAsync(LedgerPreferences preferences, CancellationToken ct)
    {
        Preferences = preferences;
        Publish(Current);
        return Task.CompletedTask;
    }

    public Task<HistoryModel?> GetHistoryAsync(string cardId, CancellationToken ct)
    {
        var card = Current.Accounts.SelectMany(a => a.Cards).FirstOrDefault(c => c.CardId == cardId);
        return Task.FromResult(card is null || card.Layout == CardLayout.Note ? null : DemoLedgerScenarios.History(card, Current.LocalNow));
    }
}

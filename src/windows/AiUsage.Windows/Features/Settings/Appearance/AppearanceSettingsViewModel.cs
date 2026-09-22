using System.Collections.ObjectModel;
using System.ComponentModel;
using AiUsage.Features.Accounts;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Settings.Appearance;

internal sealed partial class ThemeOptionViewModel(ThemePreference preference) : ObservableObject
{
    public ThemePreference Preference { get; } = preference;
    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    [ObservableProperty] public partial string Description { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsSelected { get; set; }
    /// <summary>The theme shown in the miniature preview.</summary>
    [NotifyPropertyChangedFor(nameof(PreviewIsDark))]
    [ObservableProperty] public partial EffectiveTheme PreviewTheme { get; set; }
    public bool PreviewIsDark => PreviewTheme == EffectiveTheme.Dark;
}

internal sealed partial class OrderItemViewModel(string id, AppearanceSettingsViewModel owner) : ObservableObject
{
    public string Id { get; } = id;
    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    [ObservableProperty] public partial string ProviderName { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsHidden { get; set; }
    [ObservableProperty] public partial bool IsMuted { get; set; }
    [ObservableProperty] public partial string HideLabel { get; set; } = string.Empty;
    [ObservableProperty] public partial string MuteLabel { get; set; } = string.Empty;
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [ObservableProperty] public partial bool CanMoveUp { get; set; }
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    [ObservableProperty] public partial bool CanMoveDown { get; set; }
    [ObservableProperty] public partial string AccessibleName { get; set; } = string.Empty;
    [ObservableProperty] public partial string MoveUpName { get; set; } = string.Empty;
    [ObservableProperty] public partial string MoveDownName { get; set; } = string.Empty;

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private Task MoveUpAsync() => owner.MoveAsync(Id, -1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private Task MoveDownAsync() => owner.MoveAsync(Id, 1);

    [RelayCommand]
    private Task ToggleHiddenAsync() => owner.SetHiddenAsync(this, !IsHidden);

    [RelayCommand]
    private Task ToggleMutedAsync() => owner.SetMutedAsync(this, !IsMuted);
}

/// <summary>S05 Appearance and layout. Changes apply immediately with no pending state.</summary>
internal sealed partial class AppearanceSettingsViewModel : SnapshotViewModel
{
    private readonly IPreferenceStore preferences;
    private readonly IThemeService theme;
    private bool applying;

    public AppearanceSettingsViewModel(PresentationContext context, IPreferenceStore preferences, IThemeService theme) : base(context)
    {
        this.preferences = preferences;
        this.theme = theme;
        ThemeOptions = [new(ThemePreference.System), new(ThemePreference.Light), new(ThemePreference.Dark)];
        DensityLabels = [context.Format.T("Density_Comfortable"), context.Format.T("Density_Compact")];
        theme.PropertyChanged += OnThemeChanged;
        Initialize();
    }

    public ObservableCollection<ThemeOptionViewModel> ThemeOptions { get; }
    public IReadOnlyList<string> DensityLabels { get; }
    public ObservableCollection<OrderItemViewModel> OrderItems { get; } = [];

    [ObservableProperty] public partial string ThemeNote { get; private set; } = string.Empty;
    [ObservableProperty] public partial int DensityIndex { get; set; }
    [ObservableProperty] public partial bool AlwaysOnTop { get; set; }
    [ObservableProperty] public partial bool ShowDisconnected { get; set; }
    [ObservableProperty] public partial bool HasAccounts { get; private set; }

    partial void OnDensityIndexChanged(int value)
    {
        if (!applying && value >= 0)
            _ = SetAsync(PreferenceKey.Density, (Density)value, Format.T(value == 0 ? "Announce_DensityComfortable" : "Announce_DensityCompact"));
    }

    partial void OnAlwaysOnTopChanged(bool value)
    {
        if (!applying)
            _ = SetAsync(PreferenceKey.AlwaysOnTop, value, Format.T(value ? "Announce_AlwaysOnTopOn" : "Announce_AlwaysOnTopOff"));
    }

    partial void OnShowDisconnectedChanged(bool value)
    {
        if (!applying)
            _ = SetAsync(PreferenceKey.ShowDisconnected, value, null);
    }

    private async Task SetAsync(PreferenceKey key, object value, string? announcement)
    {
        var result = await preferences.SetPreferenceAsync(new(key, value), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded && announcement is not null)
            Context.Announcer.Announce(announcement);
    }

    private void OnThemeChanged(object? sender, PropertyChangedEventArgs e) => Dispatch(() => OnSnapshot(Snapshot));

    protected override void OnSnapshot(UiSnapshot snapshot)
    {
        var format = Format;
        var prefs = snapshot.Preferences;
        applying = true;
        try
        {
            var system = theme.SystemTheme;
            var systemWord = format.T(system == EffectiveTheme.Dark ? "Theme_WordDark" : "Theme_WordLight");
            foreach (var option in ThemeOptions)
            {
                option.IsSelected = option.Preference == prefs.Theme;
                option.Label = option.Preference == ThemePreference.System ? format.F("Theme_SystemLabel", systemWord) : format.T("Theme_" + option.Preference);
                option.Description = format.T("Theme_" + option.Preference + "Description");
                option.PreviewTheme = option.Preference switch
                {
                    ThemePreference.Light => EffectiveTheme.Light,
                    ThemePreference.Dark => EffectiveTheme.Dark,
                    _ => system,
                };
            }
            ThemeNote = prefs.Theme == ThemePreference.System
                ? format.F("Theme_NoteSystem", systemWord)
                : format.F("Theme_NoteFixed", format.T("Theme_" + prefs.Theme));
            if (theme.HighContrast)
                ThemeNote += " " + format.T("Theme_NoteHighContrast");
            DensityIndex = (int)prefs.Density;
            AlwaysOnTop = prefs.AlwaysOnTop;
            ShowDisconnected = prefs.ShowDisconnected;

            var ordered = QuotaRules.Ordered(snapshot);
            HasAccounts = ordered.Count > 0;
            CollectionSync.Sync(OrderItems, ordered, a => a.Id, vm => vm.Id, a => new OrderItemViewModel(a.Id, this), (vm, a) =>
            {
                var index = ordered.ToList().IndexOf(a);
                vm.Label = a.Label;
                vm.ProviderName = Context.Providers.Get(a.ProviderId).PresentationName;
                vm.IsHidden = prefs.HiddenTargets.Contains(a.Id);
                vm.IsMuted = prefs.MutedTargets.Contains(a.Id);
                vm.HideLabel = format.T(vm.IsHidden ? "Action_Show" : "Action_Hide");
                vm.MuteLabel = format.T(vm.IsMuted ? "Action_Unmute" : "Action_Mute");
                vm.CanMoveUp = index > 0;
                vm.CanMoveDown = index < ordered.Count - 1;
                vm.AccessibleName = format.F("Order_ItemAria", a.Label, vm.ProviderName, index + 1, ordered.Count,
                    vm.IsHidden ? format.T("Order_Hidden") : string.Empty, vm.IsMuted ? format.T("Order_Muted") : string.Empty);
                vm.MoveUpName = format.F("Order_MoveUpName", a.Label);
                vm.MoveDownName = format.F("Order_MoveDownName", a.Label);
            });
        }
        finally { applying = false; }
    }

    [RelayCommand]
    private async Task SelectThemeAsync(ThemeOptionViewModel? option)
    {
        if (option is null)
            return;
        var result = await preferences.SetPreferenceAsync(new(PreferenceKey.Theme, option.Preference), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
        {
            theme.Apply(option.Preference);
            Context.Announcer.Announce(Format.F("Announce_Theme", Format.T("Theme_" + option.Preference)));
        }
    }

    internal async Task MoveAsync(string id, int direction)
    {
        var order = QuotaRules.Ordered(Snapshot).Select(a => a.Id).ToList();
        var from = order.IndexOf(id);
        var to = from + direction;
        if (from < 0 || to < 0 || to >= order.Count)
            return;
        (order[from], order[to]) = (order[to], order[from]);
        var result = await Context.Usage.ExecuteAsync(Context.Command(UiCommandKind.Reorder, null, new ReorderPayload(order)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
            Context.Announcer.Announce(Format.F("Announce_MovedTo", Snapshot.Accounts.First(a => a.Id == id).Label, to + 1, order.Count));
    }

    internal async Task SetHiddenAsync(OrderItemViewModel item, bool hidden)
    {
        var result = await Context.Usage.ExecuteAsync(Context.Command(UiCommandKind.SetVisibility, item.Id, new VisibilityPayload(hidden, false)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
            Context.Announcer.Announce(Format.T(hidden ? "Announce_Hidden" : "Announce_Shown"));
    }

    internal async Task SetMutedAsync(OrderItemViewModel item, bool muted)
    {
        var result = await Context.Usage.ExecuteAsync(Context.Command(UiCommandKind.SetMute, item.Id, new MutePayload(muted)), CancellationToken.None);
        if (result.Status == CommandStatus.Succeeded)
            Context.Announcer.Announce(Format.T(muted ? "Announce_Muted" : "Announce_Unmuted"));
    }
}

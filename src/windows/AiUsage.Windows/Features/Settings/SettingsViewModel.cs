using System.Collections.ObjectModel;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Settings;

internal sealed partial class SettingsTabViewModel(SettingsTab tab, string label) : ObservableObject
{
    public SettingsTab Tab { get; } = tab;
    public string Label { get; } = label;
}

/// <summary>Settings host: Appearance (S05), Monitoring &amp; notifications (S06), Data &amp; privacy (S10), Updates (S12).</summary>
internal sealed partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(PresentationContext context, Appearance.AppearanceSettingsViewModel appearance, Monitoring.MonitoringSettingsViewModel monitoring,
        DataPrivacy.DataPrivacyViewModel dataPrivacy, Updates.UpdatesViewModel updates)
    {
        Appearance = appearance;
        Monitoring = monitoring;
        DataPrivacy = dataPrivacy;
        Updates = updates;
        var format = context.Format;
        Tabs =
        [
            new(SettingsTab.Appearance, format.T("Settings_TabAppearance")),
            new(SettingsTab.Monitoring, format.T("Settings_TabMonitoring")),
            new(SettingsTab.DataPrivacy, format.T("Settings_TabData")),
            new(SettingsTab.Updates, format.T("Settings_TabUpdates")),
        ];
        context.Navigation.Navigated += (_, request) =>
        {
            if (request.Page == PageKey.Settings && request.Tab is { } tab)
                SelectedTab = tab;
        };
    }

    public ObservableCollection<SettingsTabViewModel> Tabs { get; }
    public Appearance.AppearanceSettingsViewModel Appearance { get; }
    public Monitoring.MonitoringSettingsViewModel Monitoring { get; }
    public DataPrivacy.DataPrivacyViewModel DataPrivacy { get; }
    public Updates.UpdatesViewModel Updates { get; }

    [NotifyPropertyChangedFor(nameof(IsAppearance), nameof(IsMonitoring), nameof(IsDataPrivacy), nameof(IsUpdates), nameof(SelectedIndex))]
    [ObservableProperty] public partial SettingsTab SelectedTab { get; set; }

    public bool IsAppearance => SelectedTab == SettingsTab.Appearance;
    public bool IsMonitoring => SelectedTab == SettingsTab.Monitoring;
    public bool IsDataPrivacy => SelectedTab == SettingsTab.DataPrivacy;
    public bool IsUpdates => SelectedTab == SettingsTab.Updates;

    public int SelectedIndex
    {
        get => (int)SelectedTab;
        set
        {
            if (value >= 0 && value < Tabs.Count)
                SelectedTab = (SettingsTab)value;
        }
    }
}

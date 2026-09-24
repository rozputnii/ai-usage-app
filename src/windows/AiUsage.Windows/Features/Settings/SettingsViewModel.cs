using AiUsage.Features.Presentation;
using AiUsage.Features.SystemStatusPage;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiUsage.Features.Settings;

/// <summary>
/// Single settings view opened from the header: Appearance (S05), Monitoring &amp; notifications (S06), Data &amp;
/// privacy (S10), Updates (S12) and System status (S09) are consecutive sections without tabs. A navigation request
/// naming a section scrolls to it.
/// </summary>
internal sealed partial class SettingsViewModel : ObservableObject
{
    public SettingsViewModel(PresentationContext context, Appearance.AppearanceSettingsViewModel appearance, Monitoring.MonitoringSettingsViewModel monitoring,
        DataPrivacy.DataPrivacyViewModel dataPrivacy, Updates.UpdatesViewModel updates, SystemStatusViewModel systemStatus)
    {
        Appearance = appearance;
        Monitoring = monitoring;
        DataPrivacy = dataPrivacy;
        Updates = updates;
        SystemStatus = systemStatus;
        context.Navigation.Navigated += (_, request) =>
        {
            if (request.Page != PageKey.Settings)
                return;
            RequestedSection = request.Tab ?? SettingsTab.Appearance;
            SectionRequested?.Invoke(this, RequestedSection);
        };
    }

    public Appearance.AppearanceSettingsViewModel Appearance { get; }
    public Monitoring.MonitoringSettingsViewModel Monitoring { get; }
    public DataPrivacy.DataPrivacyViewModel DataPrivacy { get; }
    public Updates.UpdatesViewModel Updates { get; }
    public SystemStatusViewModel SystemStatus { get; }

    /// <summary>The section the latest navigation asked for; the view scrolls to it when shown.</summary>
    public SettingsTab RequestedSection { get; private set; }

    public event EventHandler<SettingsTab>? SectionRequested;
}

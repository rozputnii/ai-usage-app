using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using AiUsage.Features.Presentation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiUsage.Features.Accounts;

internal sealed partial class AccountListItemViewModel(string id) : ObservableObject
{
    public string Id { get; } = id;
    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    [ObservableProperty] public partial string ValueText { get; set; } = string.Empty;
    [ObservableProperty] public partial ValueTone Tone { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] public partial bool IsDisconnected { get; set; }
    [ObservableProperty] public partial string AccessibleName { get; set; } = string.Empty;
}

internal sealed partial class ContextOptionViewModel(string id) : ObservableObject
{
    public string Id { get; } = id;
    [ObservableProperty] public partial string Label { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

internal sealed partial class ExtensionViewModel : ObservableObject
{
    [ObservableProperty] public partial string Title { get; set; } = string.Empty;
    [ObservableProperty] public partial string Value { get; set; } = string.Empty;
    [ObservableProperty] public partial string Note { get; set; } = string.Empty;

    /// <summary>Money only with currency and exponent; otherwise minor units without currency formatting (never inferred).</summary>
    public static ExtensionViewModel From(ExtensionItem extension, PresentationFormatter format)
    {
        var title = extension.Label + extension.Enabled switch { true => format.T("Extension_Enabled"), false => format.T("Extension_Off"), _ => string.Empty }
            + (extension.Currency is null ? format.T("Extension_CurrencyNotReported") : string.Empty);
        var money = format.Money(extension.AmountMinor, extension.Exponent, extension.Currency);
        string note;
        if (money is not null)
        {
            var limit = extension.LimitMinor is null
                ? format.T(extension.HasExplicitNullLimit ? "Extension_LimitNotSet" : "Extension_LimitNotReported")
                : format.F("Extension_LimitOf", format.Money(extension.LimitMinor, extension.Exponent, extension.Currency));
            note = format.F("Extension_MoneyNote", extension.Currency, limit);
        }
        else
            note = format.T(extension.AmountMinor is null ? "Extension_NoAmount" : "Extension_MinorUnits");
        return new ExtensionViewModel
        {
            Title = title,
            Value = money ?? (extension.AmountMinor is null ? "—" : format.NativeNumber(extension.AmountMinor)),
            Note = note,
        };
    }
}

/// <summary>Expandable quota group. Auto expands the first group and any group with a warning unless the user chose otherwise (D-116/D-117).</summary>
internal sealed partial class QuotaGroupViewModel(string id, AccountDetailViewModel owner) : ObservableObject
{
    public string Id { get; } = id;
    public ObservableCollection<QuotaWindowViewModel> Windows { get; } = [];

    [ObservableProperty] public partial string Label { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsExpanded { get; private set; }
    [ObservableProperty] public partial string ExpansionText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsSharedPool { get; private set; }
    [ObservableProperty] public partial string SharedPoolBadge { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsHidden { get; private set; }
    [ObservableProperty] public partial string HideLabel { get; private set; } = string.Empty;
    [ObservableProperty] public partial string CollapsedSummary { get; private set; } = string.Empty;
    [ObservableProperty] public partial ExpansionPreference Preference { get; private set; }

    public bool ShowCollapsedSummary => !IsExpanded;
    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(ShowCollapsedSummary));

    public void Update(AccountItem account, GroupItem group, int index, Preferences preferences, PresentationFormatter format)
    {
        Label = group.Label;
        Preference = group.Expansion;
        IsSharedPool = group.SharedPoolId is not null;
        SharedPoolBadge = format.T("Group_SharedPoolBadge");
        IsHidden = preferences.HiddenTargets.Contains(group.Id);
        HideLabel = format.T(IsHidden ? "Action_Unhide" : "Action_Hide");
        CollectionSync.Sync(Windows, group.Windows, w => w.Id, vm => vm.Id, w => new QuotaWindowViewModel(w.Id), (vm, w) => vm.Update(account, group, w, preferences, format));
        var hasWarning = Windows.Any(w => w.Severity >= QuotaSeverity.Warning);
        IsExpanded = group.Expansion switch
        {
            ExpansionPreference.Expanded => true,
            ExpansionPreference.Collapsed => false,
            _ => index == 0 || hasWarning,
        };
        ExpansionText = group.Expansion switch
        {
            ExpansionPreference.Expanded => format.T("Group_ExpandedByYou"),
            ExpansionPreference.Collapsed => format.T("Group_CollapsedByYou"),
            _ => format.T(IsExpanded ? "Group_ExpandedAuto" : "Group_CollapsedAuto"),
        };
        var lowest = Windows.Where(w => w.Kind == MeterKind.Bar && w.RemainingPercent is not null).OrderBy(w => w.RemainingPercent).FirstOrDefault();
        CollapsedSummary = lowest is null
            ? format.F(Windows.Count == 1 ? "Group_SummaryOne" : "Group_SummaryMany", Windows.Count)
            : format.F(Windows.Count == 1 ? "Group_SummaryOneLowest" : "Group_SummaryManyLowest", Windows.Count, lowest.ValueText, lowest.UnitText);
    }

    [RelayCommand]
    private Task ToggleExpansionAsync() => owner.SetExpansionAsync(this, IsExpanded ? ExpansionPreference.Collapsed : ExpansionPreference.Expanded);

    [RelayCommand]
    private Task ToggleHideAsync() => owner.ToggleHideAsync(Id, Label, IsHidden);
}

/// <summary>DataAnnotations requires a public validation type; error messages are resource keys.</summary>
public static class AccountLabelValidation
{
    public static ValidationResult? Validate(string value, ValidationContext _)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return new ValidationResult("Rename_ErrorEmpty");
        return trimmed.Length > 80 ? new ValidationResult("Rename_ErrorTooLong") : ValidationResult.Success;
    }
}

/// <summary>Inline rename with validation: trimmed, nonempty, at most 80 characters. Invalid input keeps the prior label.</summary>
internal sealed partial class RenameAccountViewModel(Func<string, Task<bool>> save, ITextResources text) : ObservableValidator
{
    [ObservableProperty] public partial bool IsEditing { get; private set; }

    [NotifyDataErrorInfo]
    [CustomValidation(typeof(AccountLabelValidation), nameof(AccountLabelValidation.Validate))]
    [ObservableProperty] public partial string LabelText { get; set; } = string.Empty;

    [ObservableProperty] public partial string ErrorText { get; private set; } = string.Empty;

    public bool HasError => ErrorText.Length > 0;
    partial void OnErrorTextChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnLabelTextChanged(string value)
    {
        if (HasError)
            ErrorText = string.Empty;
    }

    public void Begin(string current)
    {
        LabelText = current;
        ClearErrors();
        ErrorText = string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsEditing = false;
        ClearErrors();
        ErrorText = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            ErrorText = text.Get(GetErrors(nameof(LabelText)).First().ErrorMessage!);
            return;
        }
        if (await save(LabelText.Trim()))
            IsEditing = false;
    }
}

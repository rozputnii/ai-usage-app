using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.Settings.Updates;

internal sealed partial class UpdatesView : UserControl
{
    public UpdatesView() => InitializeComponent();

    public UpdatesViewModel? ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    }

    private string FailureKey(bool failure) => failure ? "CritBrush" : "TextBrush";
}

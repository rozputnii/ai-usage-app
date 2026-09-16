using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiUsage.Features.CliImport;

internal sealed partial class CliImportView : UserControl
{
    public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(CliImportView), new PropertyMetadata(null));

    public CliImportView() => InitializeComponent();

    public CliImportViewModel ViewModel
    {
        get;
        set
        {
            field = value;
            Bindings.Update();
        }
    } = null!;

    public ICommand? CloseCommand { get => (ICommand?)GetValue(CloseCommandProperty); set => SetValue(CloseCommandProperty, value); }

    /// <summary>Cancel and Import are offered while candidates are listed and before an import has run.</summary>
    private Visibility FoundVisible(bool canRescan, bool done) => canRescan && !done ? Visibility.Visible : Visibility.Collapsed;
}

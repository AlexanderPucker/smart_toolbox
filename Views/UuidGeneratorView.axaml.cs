using Avalonia.Controls;
using SmartToolbox.ViewModels;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

public partial class UuidGeneratorView : UserControl
{
    public UuidGeneratorView()
    {
        InitializeComponent();
        var vm = new UuidGeneratorViewModel();
        vm.CopyToClipboard = CopyToClipboardAsync;
        DataContext = vm;
    }

    private async Task CopyToClipboardAsync(string text)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
            await topLevel.Clipboard.SetTextAsync(text);
    }
}

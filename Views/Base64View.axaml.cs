using Avalonia.Controls;
using SmartToolbox.ViewModels;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

public partial class Base64View : UserControl
{
    public Base64View()
    {
        InitializeComponent();
        var vm = new Base64ViewModel();
        vm.CopyToClipboard = CopyToClipboardAsync;
        DataContext = vm;
    }

    private async Task CopyToClipboardAsync(string text)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
            await topLevel.Clipboard.SetTextAsync(text);
    }
}

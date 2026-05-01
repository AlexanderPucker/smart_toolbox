using Avalonia.Controls;
using SmartToolbox.ViewModels;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

public partial class JsonFormatterView : UserControl
{
    private readonly JsonFormatterViewModel _vm;

    public JsonFormatterView()
    {
        InitializeComponent();
        _vm = new JsonFormatterViewModel();
        _vm.CopyToClipboard = CopyToClipboardAsync;
        DataContext = _vm;
    }

    private async Task CopyToClipboardAsync(string text)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
            await topLevel.Clipboard.SetTextAsync(text);
    }
}

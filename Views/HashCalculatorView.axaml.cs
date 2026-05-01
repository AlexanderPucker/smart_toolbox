using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SmartToolbox.ViewModels;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

public partial class HashCalculatorView : UserControl
{
    private readonly HashCalculatorViewModel _vm;

    public HashCalculatorView()
    {
        InitializeComponent();
        _vm = new HashCalculatorViewModel();
        _vm.BrowseFile = BrowseFileAsync;
        DataContext = _vm;
    }

    private async Task<string?> BrowseFileAsync()
    {
        if (TopLevel.GetTopLevel(this) is not Window window) return null;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要计算哈希的文件",
            AllowMultiple = false
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}

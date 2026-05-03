using Avalonia.Controls;
using Avalonia.Input;

namespace SmartToolbox.Views;

public partial class ClipboardView : UserControl
{
    public ClipboardView()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ViewModels.ClipboardViewModel vm)
        {
            vm.SearchCommand.Execute(null);
        }
    }
}

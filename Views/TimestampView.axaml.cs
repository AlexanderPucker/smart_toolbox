using Avalonia.Controls;
using SmartToolbox.ViewModels;

namespace SmartToolbox.Views;

public partial class TimestampView : UserControl
{
    private readonly TimestampViewModel _vm;

    public TimestampView()
    {
        InitializeComponent();
        _vm = new TimestampViewModel();
        DataContext = _vm;
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _vm.Dispose();
    }
}

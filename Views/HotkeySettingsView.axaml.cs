using Avalonia.Controls;
using SmartToolbox.ViewModels;

namespace SmartToolbox.Views;

public partial class HotkeySettingsView : UserControl
{
    public HotkeySettingsView()
    {
        InitializeComponent();
    }

    private void OnCategorySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is HotkeySettingsViewModel vm && e.AddedItems.Count > 0)
        {
            var category = e.AddedItems[0]?.ToString();
            if (category != null)
            {
                vm.FilterByCategoryCommand.Execute(category);
            }
        }
    }
}

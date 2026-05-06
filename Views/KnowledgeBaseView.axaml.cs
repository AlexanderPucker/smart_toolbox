using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartToolbox.ViewModels;

namespace SmartToolbox.Views;

public partial class KnowledgeBaseView : UserControl
{
    public KnowledgeBaseView()
    {
        InitializeComponent();
    }

    private async void OnAddDocumentClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not KnowledgeBaseViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            vm.StatusMessage = "无法打开文件选择器";
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择文本文档",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("文本文件")
                {
                    Patterns = new[] { "*.txt", "*.md", "*.json", "*.log", "*.csv", "*.xml", "*.yml", "*.yaml" }
                },
                new FilePickerFileType("代码文件")
                {
                    Patterns = new[] { "*.cs", "*.java", "*.py", "*.js", "*.ts" }
                }
            }
        });

        var file = files.Count > 0 ? files[0] : null;
        if (file is null)
        {
            vm.StatusMessage = "已取消选择";
            return;
        }

        await vm.AddDocumentFromFileAsync(file.Path.LocalPath);
    }
}

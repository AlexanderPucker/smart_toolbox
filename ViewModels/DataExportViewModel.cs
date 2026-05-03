using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class DataExportViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _selectedFormat = "Markdown";

    [ObservableProperty]
    private string _selectedScope = "当前对话";

    [ObservableProperty]
    private bool _includeMetadata = true;

    [ObservableProperty]
    private bool _includeTimestamps = true;

    [ObservableProperty]
    private bool _includeTokenCounts;

    [ObservableProperty]
    private bool _includeCosts;

    [ObservableProperty]
    private bool _includeSystemPrompts = true;

    [ObservableProperty]
    private bool _anonymize;

    [ObservableProperty]
    private bool _compressOutput;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private bool _isExporting;

    public ObservableCollection<ExportFileItem> RecentExports { get; } = new();

    private readonly DataExportService _exportService;

    public DataExportViewModel()
    {
        _exportService = DataExportService.Instance;
        _exportService.OnExportCompleted += OnExportCompleted;
        LoadRecentExports();
    }

    private void OnExportCompleted(ExportResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsExporting = false;

            if (result.Success)
            {
                StatusMessage = $"导出成功: {result.ConversationCount} 个对话, {result.MessageCount} 条消息";
                LoadRecentExports();
            }
            else
            {
                StatusMessage = $"导出失败: {result.ErrorMessage}";
            }
        });
    }

    private void LoadRecentExports()
    {
        var exports = _exportService.GetRecentExports(20);
        RecentExports.Clear();

        foreach (var file in exports)
        {
            var fileName = Path.GetFileName(file);
            var created = File.GetCreationTime(file);

            RecentExports.Add(new ExportFileItem
            {
                FilePath = file,
                FileName = fileName,
                CreatedAt = created
            });
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        IsExporting = true;
        StatusMessage = "正在导出...";

        var format = SelectedFormat switch
        {
            "JSON" => ExportFormat.Json,
            "Markdown" => ExportFormat.Markdown,
            "HTML" => ExportFormat.Html,
            "PlainText" => ExportFormat.PlainText,
            _ => ExportFormat.Markdown
        };

        var scope = SelectedScope switch
        {
            "当前对话" => ExportScope.CurrentConversation,
            "所有对话" => ExportScope.AllConversations,
            _ => ExportScope.CurrentConversation
        };

        var options = new ExportOptions
        {
            Format = format,
            Scope = scope,
            IncludeMetadata = IncludeMetadata,
            IncludeTimestamps = IncludeTimestamps,
            IncludeTokenCounts = IncludeTokenCounts,
            IncludeCosts = IncludeCosts,
            IncludeSystemPrompts = IncludeSystemPrompts,
            Anonymize = Anonymize,
            CompressOutput = CompressOutput
        };

        await _exportService.ExportAsync(options);
    }

    [RelayCommand]
    private async Task ExportAllAsync()
    {
        IsExporting = true;
        StatusMessage = "正在导出所有数据...";

        var result = await _exportService.ExportAllDataAsync(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "smart_toolbox_backup.json"));

        if (result.Success)
        {
            StatusMessage = "全部数据已导出到文档目录";
        }
        else
        {
            StatusMessage = $"导出失败: {result.ErrorMessage}";
        }

        IsExporting = false;
    }

    [RelayCommand]
    private void OpenDirectory()
    {
        _exportService.OpenExportDirectory();
    }

    [RelayCommand]
    private void CleanOldExports()
    {
        _exportService.CleanOldExports(30);
        LoadRecentExports();
        StatusMessage = "已清理30天前的导出文件";
    }
}

public class ExportFileItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

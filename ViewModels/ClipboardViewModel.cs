using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class ClipboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private bool _autoProcess;

    [ObservableProperty]
    private ClipboardItem? _selectedItem;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ContentType? _filterType;

    public ObservableCollection<ClipboardItem> History { get; } = new();
    public ObservableCollection<ClipboardActionItem> Actions { get; } = new();
    public ObservableCollection<ClipboardStatsItem> Stats { get; } = new();

    private readonly SmartClipboardService _clipboardService;

    public ClipboardViewModel()
    {
        _clipboardService = SmartClipboardService.Instance;
        _clipboardService.OnItemCopied += OnItemCopied;
        _clipboardService.OnItemProcessed += OnItemProcessed;
        _clipboardService.OnError += OnError;

        LoadHistory();
        LoadActions();
        LoadStats();
    }

    private void OnItemCopied(ClipboardItem item)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            History.Insert(0, new ClipboardItem
            {
                Id = item.Id,
                Content = item.Content,
                Type = item.Type,
                Preview = item.Preview,
                CopiedAt = item.CopiedAt,
                SizeBytes = item.SizeBytes
            });

            if (History.Count > 100)
            {
                History.RemoveAt(History.Count - 1);
            }

            StatusMessage = $"已捕获: {item.Type}";
        });
    }

    private void OnItemProcessed(ClipboardItem item, ClipboardAction action)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var historyItem = History.FirstOrDefault(h => h.Id == item.Id);
            if (historyItem != null)
            {
                historyItem.ProcessedContent = item.ProcessedContent;
            }
            StatusMessage = $"已处理: {action.Name}";
        });
    }

    private void OnError(string error)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = error;
        });
    }

    private void LoadHistory()
    {
        var history = _clipboardService.GetHistory(50);
        History.Clear();
        foreach (var item in history)
        {
            History.Add(new ClipboardItem
            {
                Id = item.Id,
                Content = item.Content,
                Type = item.Type,
                Preview = item.Preview,
                CopiedAt = item.CopiedAt,
                IsFavorite = item.IsFavorite,
                Tags = item.Tags,
                ProcessedContent = item.ProcessedContent,
                SizeBytes = item.SizeBytes
            });
        }
    }

    private void LoadActions()
    {
        var actions = _clipboardService.GetActions();
        Actions.Clear();
        foreach (var action in actions)
        {
            Actions.Add(new ClipboardActionItem
            {
                Id = action.Id,
                Name = action.Name,
                Description = action.Description,
                ApplicableType = action.ApplicableType?.ToString() ?? "全部"
            });
        }
    }

    private void LoadStats()
    {
        var stats = _clipboardService.GetStats();
        Stats.Clear();
        Stats.Add(new ClipboardStatsItem { Label = "总条目", Value = stats.TotalItems.ToString() });
        Stats.Add(new ClipboardStatsItem { Label = "收藏", Value = stats.FavoritesCount.ToString() });
        Stats.Add(new ClipboardStatsItem { Label = "今日", Value = stats.TodayCount.ToString() });
    }

    [RelayCommand]
    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _clipboardService.StopMonitoring();
            IsMonitoring = false;
            StatusMessage = "已停止监控";
        }
        else
        {
            _clipboardService.StartMonitoring();
            IsMonitoring = true;
            StatusMessage = "正在监控剪贴板";
        }
    }

    partial void OnAutoProcessChanged(bool value)
    {
        _clipboardService.AutoProcess = value;
        StatusMessage = value ? "已启用自动处理" : "已禁用自动处理";
    }

    [RelayCommand]
    private async Task ProcessItemAsync(string actionId)
    {
        if (SelectedItem == null)
        {
            StatusMessage = "请选择一个条目";
            return;
        }

        StatusMessage = "正在处理...";
        var result = await _clipboardService.ProcessItemAsync(SelectedItem.Id, actionId);

        if (!string.IsNullOrEmpty(result))
        {
            SelectedItem.ProcessedContent = result;
            StatusMessage = "处理完成";
        }
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (SelectedItem != null)
        {
            _clipboardService.ToggleFavorite(SelectedItem.Id);
            SelectedItem.IsFavorite = !SelectedItem.IsFavorite;
            StatusMessage = SelectedItem.IsFavorite ? "已收藏" : "已取消收藏";
        }
    }

    [RelayCommand]
    private void DeleteItem()
    {
        if (SelectedItem != null)
        {
            _clipboardService.DeleteItem(SelectedItem.Id);
            History.Remove(SelectedItem);
            StatusMessage = "已删除";
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _clipboardService.ClearHistory();
        History.Clear();
        StatusMessage = "历史已清空";
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            LoadHistory();
            return;
        }

        var results = _clipboardService.SearchHistory(SearchQuery);
        History.Clear();
        foreach (var item in results.Take(50))
        {
            History.Add(new ClipboardItem
            {
                Id = item.Id,
                Content = item.Content,
                Type = item.Type,
                Preview = item.Preview,
                CopiedAt = item.CopiedAt,
                IsFavorite = item.IsFavorite
            });
        }
        StatusMessage = $"找到 {History.Count} 个结果";
    }

    [RelayCommand]
    private void FilterByType(ContentType? type)
    {
        FilterType = type;
        var history = _clipboardService.GetHistory(50, type);
        History.Clear();
        foreach (var item in history)
        {
            History.Add(new ClipboardItem
            {
                Id = item.Id,
                Content = item.Content,
                Type = item.Type,
                Preview = item.Preview,
                CopiedAt = item.CopiedAt,
                IsFavorite = item.IsFavorite
            });
        }
        StatusMessage = type == null ? "显示全部" : $"筛选: {type}";
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (SelectedItem != null)
        {
            StatusMessage = "已复制到剪贴板";
        }
    }

    [RelayCommand]
    private void CopyProcessed()
    {
        if (SelectedItem?.ProcessedContent != null)
        {
            StatusMessage = "已复制处理结果";
        }
    }
}

public class ClipboardItem
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public string? Preview { get; set; }
    public DateTime CopiedAt { get; set; }
    public int AccessCount { get; set; }
    public bool IsFavorite { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ProcessedContent { get; set; }
    public int SizeBytes { get; set; }
}

public class ClipboardActionItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApplicableType { get; set; } = string.Empty;
}

public class ClipboardStatsItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

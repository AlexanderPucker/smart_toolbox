using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class OllamaViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _baseUrl = "http://localhost:11434";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "未连接";

    [ObservableProperty]
    private LocalModelItem? _selectedModel;

    [ObservableProperty]
    private string _newModelName = string.Empty;

    [ObservableProperty]
    private string _pullProgress = string.Empty;

    [ObservableProperty]
    private bool _isPulling;

    public ObservableCollection<LocalModelItem> Models { get; } = new();
    public ObservableCollection<string> RecommendedModels { get; } = new();

    private readonly OllamaService _ollamaService;

    public OllamaViewModel()
    {
        _ollamaService = OllamaService.Instance;
        _ollamaService.OnModelsUpdated += OnModelsUpdated;
        _ollamaService.OnPullProgress += OnPullProgress;
        _ollamaService.OnConnectionStatusChanged += OnConnectionStatusChanged;

        LoadRecommendedModels();
    }

    private void OnModelsUpdated(List<LocalModelInfo> models)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Models.Clear();
            foreach (var model in models)
            {
                Models.Add(new LocalModelItem
                {
                    Name = model.Name,
                    Size = FormatSize(model.Size),
                    ModifiedAt = model.ModifiedAt,
                    Description = _ollamaService.GetModelDescription(model.Name)
                });
            }
            StatusMessage = $"已加载 {Models.Count} 个模型";
        });
    }

    private void OnPullProgress(string modelName, OllamaModelPullProgress progress)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (progress.Total.HasValue && progress.Completed.HasValue)
            {
                var percent = (double)progress.Completed.Value / progress.Total.Value * 100;
                PullProgress = $"{progress.Status} ({percent:F1}%)";
            }
            else
            {
                PullProgress = progress.Status;
            }
        });
    }

    private void OnConnectionStatusChanged(bool connected)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsConnected = connected;
            StatusMessage = connected ? "已连接" : "连接失败";
        });
    }

    private void LoadRecommendedModels()
    {
        var recommended = _ollamaService.GetRecommendedModels();
        RecommendedModels.Clear();
        foreach (var model in recommended)
        {
            RecommendedModels.Add(model);
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        IsLoading = true;
        StatusMessage = "正在连接...";

        try
        {
            _ollamaService.Configure(BaseUrl);
            var connected = await _ollamaService.CheckConnectionAsync();

            if (connected)
            {
                await _ollamaService.ListModelsAsync();
            }
            else
            {
                StatusMessage = "连接失败，请确保 Ollama 正在运行";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshModelsAsync()
    {
        if (!IsConnected)
        {
            StatusMessage = "请先连接到 Ollama";
            return;
        }

        IsLoading = true;
        await _ollamaService.ListModelsAsync();
        IsLoading = false;
    }

    [RelayCommand]
    private async Task PullModelAsync()
    {
        if (string.IsNullOrWhiteSpace(NewModelName))
        {
            StatusMessage = "请输入模型名称";
            return;
        }

        if (!IsConnected)
        {
            StatusMessage = "请先连接到 Ollama";
            return;
        }

        IsPulling = true;
        PullProgress = "开始下载...";

        try
        {
            await _ollamaService.PullModelAsync(NewModelName);
            StatusMessage = $"模型 {NewModelName} 下载完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"下载失败: {ex.Message}";
        }
        finally
        {
            IsPulling = false;
            PullProgress = string.Empty;
        }
    }

    [RelayCommand]
    private async Task DeleteModelAsync()
    {
        if (SelectedModel == null)
        {
            StatusMessage = "请选择要删除的模型";
            return;
        }

        try
        {
            await _ollamaService.DeleteModelAsync(SelectedModel.Name);
            StatusMessage = $"模型 {SelectedModel.Name} 已删除";
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectRecommendedModel(string modelName)
    {
        NewModelName = modelName;
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}

public class LocalModelItem
{
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string ModifiedAt { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

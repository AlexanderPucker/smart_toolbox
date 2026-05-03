using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AISummaryViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _summaryLength = "简短";

    [ObservableProperty]
    private string _statusMessage = "输入文本进行摘要";

    public ObservableCollection<string> SummaryLengths { get; } = new()
    {
        "简短",
        "中等",
        "详细"
    };

    private readonly AIService _aiService;

    public AISummaryViewModel()
    {
        _aiService = new AIService();
        var config = AIConfigManager.LoadConfig();
        _aiService.Configure(config);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task GenerateSummaryAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusMessage = "请输入需要摘要的文本";
            return;
        }

        StatusMessage = "正在生成摘要...";
        OutputText = string.Empty;

        var lengthInstruction = SummaryLength switch
        {
            "简短" => "请用一句话概括核心内容（50字以内）",
            "中等" => "请用3-5个要点总结核心内容（200字以内）",
            "详细" => "请详细总结文本的主要内容和关键信息（500字以内）",
            _ => "请总结核心内容"
        };

        var prompt = $@"请对以下文本进行摘要：

{InputText}

{lengthInstruction}";

        var systemPrompt = "你是一个专业的文本摘要助手，擅长从文本中提取核心信息和关键要点。";

        try
        {
            OutputText = await _aiService.SendMessageAsync(prompt, systemPrompt);
            StatusMessage = $"摘要生成成功 ({InputText.Length} 字符 → {OutputText.Length} 字符)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CopyOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText)) return;
        StatusMessage = "已复制到剪贴板";
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusMessage = "已清空";
    }
}

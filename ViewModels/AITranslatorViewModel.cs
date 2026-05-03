using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AITranslatorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _sourceLanguage = "自动检测";

    [ObservableProperty]
    private string _targetLanguage = "英语";

    [ObservableProperty]
    private string _statusMessage = "输入文本进行翻译";

    public ObservableCollection<string> Languages { get; } = new()
    {
        "自动检测",
        "中文",
        "英语",
        "日语",
        "韩语",
        "法语",
        "德语",
        "西班牙语",
        "俄语",
        "阿拉伯语"
    };

    private readonly AIService _aiService;

    public AITranslatorViewModel()
    {
        _aiService = new AIService();
        var config = AIConfigManager.LoadConfig();
        _aiService.Configure(config);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusMessage = "请输入需要翻译的文本";
            return;
        }

        StatusMessage = "正在翻译...";
        OutputText = string.Empty;

        var sourceLang = SourceLanguage == "自动检测" ? "自动检测原文语言" : $"从{SourceLanguage}";
        var prompt = $@"请将以下文本从{sourceLang}翻译为{TargetLanguage}：

{InputText}

请直接给出翻译结果，不要添加额外说明。";

        var systemPrompt = "你是一个专业的多语言翻译助手，擅长准确翻译各种语言的文本。";

        try
        {
            OutputText = await _aiService.SendMessageAsync(prompt, systemPrompt);
            StatusMessage = $"翻译成功 ({InputText.Length} 字符 → {OutputText.Length} 字符)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"翻译失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SwapLanguages()
    {
        if (SourceLanguage != "自动检测")
        {
            var temp = SourceLanguage;
            SourceLanguage = TargetLanguage;
            TargetLanguage = temp;
            
            if (!string.IsNullOrWhiteSpace(OutputText))
            {
                (InputText, OutputText) = (OutputText, InputText);
            }
        }
        StatusMessage = "已交换语言方向";
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusMessage = "已清空";
    }
}

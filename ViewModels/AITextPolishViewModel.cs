using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AITextPolishViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _polishStyle = "改善语法";

    [ObservableProperty]
    private string _statusMessage = "输入文本进行润色";

    public ObservableCollection<string> PolishStyles { get; } = new()
    {
        "改善语法",
        "提升正式度",
        "简化表达",
        "学术化",
        "口语化",
        "商务化",
        "创意改写"
    };

    private readonly AIService _aiService;

    public AITextPolishViewModel()
    {
        _aiService = new AIService();
        var config = AIConfigManager.LoadConfig();
        _aiService.Configure(config);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task PolishTextAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusMessage = "请输入需要润色的文本";
            return;
        }

        StatusMessage = "正在润色...";
        OutputText = string.Empty;

        var styleInstruction = PolishStyle switch
        {
            "改善语法" => "请修正语法错误，改善标点符号使用，保持原意不变",
            "提升正式度" => "请将文本改写为更正式、专业的表达，使用书面语",
            "简化表达" => "请简化文本，去除冗余内容，使表达更清晰直接",
            "学术化" => "请将文本改写为学术风格，使用学术用语和严谨表达",
            "口语化" => "请将文本改写为更口语化、亲切的表达",
            "商务化" => "请将文本改写为商务风格，专业且礼貌",
            "创意改写" => "请在保持原意的基础上，增加文采和创意表达",
            _ => "请改善文本质量"
        };

        var prompt = $@"请对以下文本进行润色：

{InputText}

{styleInstruction}。

请直接给出润色后的文本。";

        var systemPrompt = "你是一个专业的文本润色助手，擅长改善各种类型文本的表达质量。";

        try
        {
            OutputText = await _aiService.SendMessageAsync(prompt, systemPrompt);
            StatusMessage = "文本润色完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"润色失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusMessage = "已清空";
    }
}

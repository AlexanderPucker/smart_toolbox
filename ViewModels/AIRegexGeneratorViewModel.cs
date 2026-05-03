using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AIRegexGeneratorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputDescription = string.Empty;

    [ObservableProperty]
    private string _outputRegex = string.Empty;

    [ObservableProperty]
    private string _testText = string.Empty;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "用自然语言描述你需要匹配的内容";

    private readonly AIService _aiService;

    public ObservableCollection<string> Examples { get; } = new()
    {
        "匹配邮箱地址",
        "匹配手机号码",
        "匹配URL链接",
        "匹配IPv4地址",
        "匹配日期格式(YYYY-MM-DD)",
        "匹配HTML标签"
    };

    public AIRegexGeneratorViewModel()
    {
        _aiService = new AIService();
        var config = AIConfigManager.LoadConfig();
        _aiService.Configure(config);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task GenerateRegexAsync()
    {
        if (string.IsNullOrWhiteSpace(InputDescription))
        {
            StatusMessage = "请输入正则表达式的需求描述";
            return;
        }

        StatusMessage = "正在生成正则表达式...";
        OutputRegex = string.Empty;

        var prompt = $@"请根据以下需求生成正则表达式：

需求：{InputDescription}

请只提供正则表达式，不要其他说明。如果可能，请添加注释说明各部分的作用。";

        var systemPrompt = "你是一个正则表达式专家，擅长根据需求生成准确的正则表达式。";

        try
        {
            OutputRegex = await _aiService.SendMessageAsync(prompt, systemPrompt);
            StatusMessage = "正则表达式生成完成，可以输入测试文本进行验证";
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TestRegex()
    {
        if (string.IsNullOrWhiteSpace(OutputRegex))
        {
            StatusMessage = "请先生成正则表达式";
            return;
        }

        if (string.IsNullOrWhiteSpace(TestText))
        {
            StatusMessage = "请输入测试文本";
            return;
        }

        try
        {
            var regex = new Regex(OutputRegex);
            var matches = regex.Matches(TestText);
            
            if (matches.Count > 0)
            {
                TestResult = $"匹配成功！找到 {matches.Count} 个匹配：\n\n";
                for (int i = 0; i < matches.Count; i++)
                {
                    TestResult += $"[{i + 1}] {matches[i].Value}\n";
                }
                StatusMessage = $"测试通过，共匹配 {matches.Count} 次";
            }
            else
            {
                TestResult = "未找到匹配项";
                StatusMessage = "测试完成，无匹配结果";
            }
        }
        catch (Exception ex)
        {
            TestResult = $"正则表达式错误: {ex.Message}";
            StatusMessage = "正则表达式格式错误";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputDescription = string.Empty;
        OutputRegex = string.Empty;
        TestText = string.Empty;
        TestResult = string.Empty;
        StatusMessage = "已清空";
    }
}

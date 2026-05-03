using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AICodeExplainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputCode = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _selectedLanguage = "自动检测";

    [ObservableProperty]
    private string _statusMessage = "输入代码进行解释";

    [ObservableProperty]
    private string _explanationLevel = "详细";

    public ObservableCollection<string> Languages { get; } = new()
    {
        "自动检测",
        "Python",
        "JavaScript",
        "Java",
        "C#",
        "C++",
        "Go",
        "Rust",
        "TypeScript",
        "PHP",
        "Ruby",
        "Swift"
    };

    public ObservableCollection<string> ExplanationLevels { get; } = new()
    {
        "简要",
        "详细",
        "逐行解释"
    };

    private readonly AIService _aiService;

    public AICodeExplainViewModel()
    {
        _aiService = new AIService();
        var config = AIConfigManager.LoadConfig();
        _aiService.Configure(config);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ExplainCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(InputCode))
        {
            StatusMessage = "请输入需要解释的代码";
            return;
        }

        StatusMessage = "正在分析代码...";
        OutputText = string.Empty;

        var languageText = SelectedLanguage == "自动检测" ? "自动检测" : SelectedLanguage;
        var levelInstruction = ExplanationLevel switch
        {
            "简要" => "请简要说明代码的主要功能和作用（100字以内）",
            "详细" => "请详细解释代码的功能、结构和关键逻辑",
            "逐行解释" => "请逐行或逐块解释代码的作用",
            _ => "请解释代码的功能"
        };

        var prompt = $@"请解释以下{languageText}代码：

```{languageText}
{InputCode}
```

{levelInstruction}

请包含以下方面：
1. 代码的主要功能
2. 关键逻辑和算法
3. 输入输出说明
4. 潜在的改进建议";

        var systemPrompt = "你是一个专业的代码解释助手，擅长用通俗易懂的语言解释各种编程语言的功能。";

        try
        {
            OutputText = await _aiService.SendMessageAsync(prompt, systemPrompt);
            StatusMessage = "代码解释完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"解释失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputCode = string.Empty;
        OutputText = string.Empty;
        StatusMessage = "已清空";
    }
}

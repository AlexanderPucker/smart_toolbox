using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class CodeSandboxViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _codeInput = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _errorText = string.Empty;

    [ObservableProperty]
    private string _selectedLanguage = "Python";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _executionSuccess;

    [ObservableProperty]
    private int _exitCode;

    [ObservableProperty]
    private double _executionTime;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private int _timeoutSeconds = 10;

    public ObservableCollection<string> Languages { get; } = new()
    {
        "Python", "JavaScript", "TypeScript", "Java", "CSharp", "Go", "Ruby", "PHP"
    };

    public ObservableCollection<LanguageStatusItem> LanguageStatuses { get; } = new();

    private readonly CodeSandboxService _sandbox;
    private CancellationTokenSource? _cancellationTokenSource;

    public CodeSandboxViewModel()
    {
        _sandbox = CodeSandboxService.Instance;
        _sandbox.OnCodeExecuted += OnCodeExecuted;
        CheckLanguagesAsync();
        LoadTemplate();
    }

    private void OnCodeExecuted(SupportedLanguage language, ExecutionResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = result.Success
                ? $"执行完成 ({result.Duration.TotalMilliseconds:F0}ms)"
                : $"执行失败: {result.Error}";
        });
    }

    private async void CheckLanguagesAsync()
    {
        var available = await _sandbox.CheckAllLanguagesAsync();
        LanguageStatuses.Clear();

        foreach (var (language, isAvailable) in available)
        {
            LanguageStatuses.Add(new LanguageStatusItem
            {
                Language = language.ToString(),
                IsAvailable = isAvailable,
                StatusIcon = isAvailable ? "✅" : "❌"
            });
        }
    }

    private void LoadTemplate()
    {
        if (Enum.TryParse<SupportedLanguage>(SelectedLanguage, out var language))
        {
            CodeInput = _sandbox.GetLanguageTemplate(language);
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        if (Enum.TryParse<SupportedLanguage>(value, out var language))
        {
            CodeInput = _sandbox.GetLanguageTemplate(language);
        }
    }

    [RelayCommand]
    private async Task RunCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(CodeInput))
        {
            StatusMessage = "请输入代码";
            return;
        }

        if (!Enum.TryParse<SupportedLanguage>(SelectedLanguage, out var language))
        {
            StatusMessage = "不支持的语言";
            return;
        }

        IsRunning = true;
        OutputText = string.Empty;
        ErrorText = string.Empty;
        ExecutionSuccess = false;
        StatusMessage = "正在执行...";

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var options = new CodeSandboxOptions
            {
                TimeoutMs = TimeoutSeconds * 1000,
                CaptureOutput = true
            };

            var result = await _sandbox.ExecuteAsync(language, CodeInput, options, _cancellationTokenSource.Token);

            OutputText = result.Output;
            ErrorText = result.Error;
            ExecutionSuccess = result.Success;
            ExitCode = result.ExitCode;
            ExecutionTime = result.Duration.TotalSeconds;

            if (result.TimedOut)
            {
                StatusMessage = $"执行超时 (>{TimeoutSeconds}s)";
            }
            else if (result.Success)
            {
                StatusMessage = $"执行成功 ({result.Duration.TotalMilliseconds:F0}ms)";
            }
            else
            {
                StatusMessage = $"执行失败 (退出码: {result.ExitCode})";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "执行已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"执行错误: {ex.Message}";
            ErrorText = ex.Message;
        }
        finally
        {
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void StopExecution()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "正在停止...";
    }

    [RelayCommand]
    private void ClearCode()
    {
        CodeInput = string.Empty;
        OutputText = string.Empty;
        ErrorText = string.Empty;
        StatusMessage = "已清空";
    }

    [RelayCommand]
    private void LoadTemplateCommand()
    {
        LoadTemplate();
        StatusMessage = "已加载模板";
    }

    [RelayCommand]
    private async Task ExplainCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(CodeInput))
        {
            StatusMessage = "请输入代码";
            return;
        }

        StatusMessage = "正在分析代码...";

        try
        {
            var aiService = AIService.Instance;
            var prompt = $@"请解释以下 {SelectedLanguage} 代码：

```
{CodeInput}
```

请说明：
1. 代码的功能
2. 关键逻辑
3. 可能的改进建议";

            var response = await aiService.SendMessageAsync(prompt, "你是一个专业的代码解释助手。");

            OutputText = response.Content;
            StatusMessage = "代码分析完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"分析失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task FixCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(CodeInput))
        {
            StatusMessage = "请输入代码";
            return;
        }

        if (string.IsNullOrEmpty(ErrorText))
        {
            StatusMessage = "没有错误需要修复";
            return;
        }

        StatusMessage = "正在修复代码...";

        try
        {
            var aiService = AIService.Instance;
            var prompt = $@"以下 {SelectedLanguage} 代码有错误，请修复：

```
{CodeInput}
```

错误信息：
{ErrorText}

请提供修复后的完整代码：";

            var response = await aiService.SendMessageAsync(prompt, "你是一个专业的代码调试助手。");

            CodeInput = response.Content;
            StatusMessage = "代码已修复，请重新运行";
        }
        catch (Exception ex)
        {
            StatusMessage = $"修复失败: {ex.Message}";
        }
    }
}

public class LanguageStatusItem
{
    public string Language { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string StatusIcon { get; set; } = string.Empty;
}

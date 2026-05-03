using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class PromptOptimizerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _originalPrompt = string.Empty;

    [ObservableProperty]
    private string _optimizedPrompt = string.Empty;

    [ObservableProperty]
    private string _analysisOutput = string.Empty;

    [ObservableProperty]
    private string _selectedOptimizationType = "综合优化";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private double _clarityScore;

    [ObservableProperty]
    private double _specificityScore;

    [ObservableProperty]
    private double _structureScore;

    [ObservableProperty]
    private double _overallScore;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private PromptVariantItem? _selectedVariant;

    public ObservableCollection<string> OptimizationTypes { get; } = new()
    {
        "综合优化", "清晰度优化", "具体性优化", "结构优化", "思维链优化", "角色扮演优化", "输出格式优化"
    };

    public ObservableCollection<PromptVariantItem> Variants { get; } = new();
    public ObservableCollection<string> Issues { get; } = new();
    public ObservableCollection<string> Suggestions { get; } = new();
    public ObservableCollection<ABTestResultItem> TestHistory { get; } = new();

    private readonly PromptOptimizerService _optimizer;

    public PromptOptimizerViewModel()
    {
        _optimizer = PromptOptimizerService.Instance;
        _optimizer.OnABTestCompleted += OnABTestCompleted;
        LoadTestHistory();
    }

    private void OnABTestCompleted(ABTestResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TestHistory.Insert(0, new ABTestResultItem
            {
                TestId = result.TestId,
                CreatedAt = result.CreatedAt,
                Conclusion = result.Conclusion ?? ""
            });
        });
    }

    private void LoadTestHistory()
    {
        var history = _optimizer.GetTestHistory();
        TestHistory.Clear();

        foreach (var test in history.Take(10))
        {
            TestHistory.Add(new ABTestResultItem
            {
                TestId = test.TestId,
                CreatedAt = test.CreatedAt,
                Conclusion = test.Conclusion ?? ""
            });
        }
    }

    [RelayCommand]
    private async Task AnalyzePromptAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalPrompt))
        {
            StatusMessage = "请输入提示词";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在分析...";

        try
        {
            var analysis = await _optimizer.AnalyzePromptAsync(OriginalPrompt);

            ClarityScore = analysis.ClarityScore * 100;
            SpecificityScore = analysis.SpecificityScore * 100;
            StructureScore = analysis.StructureScore * 100;
            OverallScore = analysis.OverallScore * 100;

            Issues.Clear();
            foreach (var issue in analysis.Issues)
            {
                Issues.Add(issue);
            }

            Suggestions.Clear();
            foreach (var suggestion in analysis.Suggestions)
            {
                Suggestions.Add(suggestion);
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# 提示词分析报告");
            sb.AppendLine();
            sb.AppendLine($"## 评分");
            sb.AppendLine($"- 清晰度: {ClarityScore:F0}%");
            sb.AppendLine($"- 具体性: {SpecificityScore:F0}%");
            sb.AppendLine($"- 结构性: {StructureScore:F0}%");
            sb.AppendLine($"- 综合评分: {OverallScore:F0}%");
            sb.AppendLine();

            if (Issues.Count > 0)
            {
                sb.AppendLine("## 发现的问题");
                foreach (var issue in Issues)
                {
                    sb.AppendLine($"- {issue}");
                }
                sb.AppendLine();
            }

            if (Suggestions.Count > 0)
            {
                sb.AppendLine("## 改进建议");
                foreach (var suggestion in Suggestions)
                {
                    sb.AppendLine($"- {suggestion}");
                }
            }

            AnalysisOutput = sb.ToString();
            StatusMessage = "分析完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"分析失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OptimizePromptAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalPrompt))
        {
            StatusMessage = "请输入提示词";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在优化...";

        try
        {
            var type = SelectedOptimizationType switch
            {
                "清晰度优化" => PromptOptimizationType.Clarity,
                "具体性优化" => PromptOptimizationType.Specificity,
                "结构优化" => PromptOptimizationType.Structure,
                "思维链优化" => PromptOptimizationType.ChainOfThought,
                "角色扮演优化" => PromptOptimizationType.RolePlay,
                "输出格式优化" => PromptOptimizationType.OutputFormat,
                _ => PromptOptimizationType.Clarity
            };

            var variant = await _optimizer.OptimizePromptAsync(OriginalPrompt, type);
            OptimizedPrompt = variant.Prompt;

            StatusMessage = "优化完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"优化失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GenerateVariantsAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalPrompt))
        {
            StatusMessage = "请输入提示词";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在生成变体...";

        try
        {
            var variants = await _optimizer.GenerateVariantsAsync(OriginalPrompt, 4);

            Variants.Clear();
            foreach (var variant in variants)
            {
                Variants.Add(new PromptVariantItem
                {
                    Id = variant.Id,
                    Prompt = variant.Prompt,
                    Description = variant.Description,
                    Score = variant.Score
                });
            }

            StatusMessage = $"已生成 {Variants.Count} 个变体";
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RunABTestAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalPrompt))
        {
            StatusMessage = "请输入提示词";
            return;
        }

        IsLoading = true;
        StatusMessage = "正在进行 A/B 测试...";

        try
        {
            var result = await _optimizer.RunABTestAsync(OriginalPrompt, "测试输入");

            StatusMessage = $"A/B 测试完成: {result.Conclusion}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"测试失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EnhanceWithChainOfThoughtAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalPrompt))
        {
            StatusMessage = "请输入提示词";
            return;
        }

        OptimizedPrompt = await _optimizer.EnhanceWithChainOfThoughtAsync(OriginalPrompt);
        StatusMessage = "已添加思维链";
    }

    [RelayCommand]
    private void UseOptimizedPrompt()
    {
        if (!string.IsNullOrWhiteSpace(OptimizedPrompt))
        {
            OriginalPrompt = OptimizedPrompt;
            StatusMessage = "已应用优化后的提示词";
        }
    }

    [RelayCommand]
    private void UseVariant()
    {
        if (SelectedVariant != null)
        {
            OriginalPrompt = SelectedVariant.Prompt;
            StatusMessage = "已应用选中的变体";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        OriginalPrompt = string.Empty;
        OptimizedPrompt = string.Empty;
        AnalysisOutput = string.Empty;
        Variants.Clear();
        Issues.Clear();
        Suggestions.Clear();
        StatusMessage = "已清空";
    }
}

public class PromptVariantItem
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double? Score { get; set; }
}

public class ABTestResultItem
{
    public string TestId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Conclusion { get; set; } = string.Empty;
}

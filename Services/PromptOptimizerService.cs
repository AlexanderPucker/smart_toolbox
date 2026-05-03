using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public enum PromptOptimizationType
{
    Clarity,
    Specificity,
    Structure,
    ChainOfThought,
    RolePlay,
    OutputFormat,
    Constraint,
    Examples
}

public class PromptAnalysis
{
    public double ClarityScore { get; set; }
    public double SpecificityScore { get; set; }
    public double StructureScore { get; set; }
    public double OverallScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class PromptVariant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Prompt { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PromptOptimizationType> AppliedOptimizations { get; set; } = new();
    public double? Score { get; set; }
    public string? Feedback { get; set; }
}

public class ABTestResult
{
    public string TestId { get; set; } = Guid.NewGuid().ToString();
    public string OriginalPrompt { get; set; } = string.Empty;
    public List<PromptVariant> Variants { get; set; } = new();
    public string WinningVariantId { get; set; } = string.Empty;
    public Dictionary<string, double> Scores { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Conclusion { get; set; }
}

public sealed class PromptOptimizerService
{
    private static readonly Lazy<PromptOptimizerService> _instance = new(() => new PromptOptimizerService());
    public static PromptOptimizerService Instance => _instance.Value;

    private readonly AIService _aiService;
    private readonly List<ABTestResult> _testHistory = new();

    public event Action<ABTestResult>? OnABTestCompleted;

    private PromptOptimizerService()
    {
        _aiService = AIService.Instance;
    }

    public async Task<PromptAnalysis> AnalyzePromptAsync(string prompt)
    {
        var analysis = new PromptAnalysis();

        analysis.ClarityScore = AnalyzeClarity(prompt);
        analysis.SpecificityScore = AnalyzeSpecificity(prompt);
        analysis.StructureScore = AnalyzeStructure(prompt);
        analysis.OverallScore = (analysis.ClarityScore + analysis.SpecificityScore + analysis.StructureScore) / 3;

        GenerateSuggestions(analysis, prompt);

        return analysis;
    }

    private double AnalyzeClarity(string prompt)
    {
        double score = 1.0;

        if (string.IsNullOrWhiteSpace(prompt))
            return 0;

        if (prompt.Length < 10)
            score -= 0.3;

        var sentences = prompt.Split(new[] { '.', '!', '?', '。', '！', '？' }, StringSplitOptions.RemoveEmptyEntries);
        var avgSentenceLength = sentences.Length > 0 ? prompt.Length / sentences.Length : prompt.Length;

        if (avgSentenceLength > 100)
            score -= 0.2;

        var vagueWords = new[] { "something", "somehow", "maybe", "可能", "一些", "某种" };
        foreach (var word in vagueWords)
        {
            if (prompt.ToLower().Contains(word))
                score -= 0.1;
        }

        return Math.Max(0, Math.Min(1, score));
    }

    private double AnalyzeSpecificity(string prompt)
    {
        double score = 0.5;

        var specificIndicators = new[] { "step by step", "具体", "详细", "例如", "example", "format", "格式", "必须", "must", "should" };
        foreach (var indicator in specificIndicators)
        {
            if (prompt.ToLower().Contains(indicator))
                score += 0.1;
        }

        if (prompt.Contains("1.") || prompt.Contains("1、") || prompt.Contains("•"))
            score += 0.1;

        if (prompt.Contains("?") || prompt.Contains("？"))
            score += 0.1;

        return Math.Max(0, Math.Min(1, score));
    }

    private double AnalyzeStructure(string prompt)
    {
        double score = 0.3;

        if (prompt.Contains("\n\n") || prompt.Contains("\n\n"))
            score += 0.2;

        if (prompt.Contains("1.") && prompt.Contains("2."))
            score += 0.2;

        if (prompt.Contains("```"))
            score += 0.2;

        var sections = new[] { "背景", "任务", "要求", "background", "task", "requirement", "context", "instruction" };
        foreach (var section in sections)
        {
            if (prompt.ToLower().Contains(section))
                score += 0.05;
        }

        return Math.Max(0, Math.Min(1, score));
    }

    private void GenerateSuggestions(PromptAnalysis analysis, string prompt)
    {
        if (analysis.ClarityScore < 0.7)
        {
            analysis.Issues.Add("提示词不够清晰");
            analysis.Suggestions.Add("尝试使用更直接、明确的语言表达你的需求");
        }

        if (analysis.SpecificityScore < 0.7)
        {
            analysis.Issues.Add("提示词缺乏具体性");
            analysis.Suggestions.Add("添加具体的例子、格式要求或约束条件");
        }

        if (analysis.StructureScore < 0.7)
        {
            analysis.Issues.Add("提示词结构不够清晰");
            analysis.Suggestions.Add("使用分段、编号或标题来组织你的提示词");
        }

        if (prompt.Length < 20)
        {
            analysis.Suggestions.Add("提示词过短，考虑添加更多上下文信息");
        }

        if (!prompt.Contains("输出") && !prompt.Contains("output") && !prompt.Contains("结果"))
        {
            analysis.Suggestions.Add("明确说明你期望的输出格式");
        }
    }

    public async Task<PromptVariant> OptimizePromptAsync(
        string originalPrompt,
        PromptOptimizationType optimizationType,
        string? context = null)
    {
        var optimizationInstruction = GetOptimizationInstruction(optimizationType);

        var systemPrompt = @"你是一个专业的提示词优化专家。你的任务是改进用户提供的提示词，使其更加有效。
请直接输出优化后的提示词，不要添加额外的解释。";

        var userPrompt = $@"请根据以下优化方向改进这个提示词：

原始提示词：
{originalPrompt}

优化方向：{optimizationInstruction}

{(string.IsNullOrEmpty(context) ? "" : $"上下文信息：{context}")}

请输出优化后的提示词：";

        var response = await _aiService.SendMessageAsync(userPrompt, systemPrompt);

        return new PromptVariant
        {
            Prompt = response.Content,
            Description = GetOptimizationDescription(optimizationType),
            AppliedOptimizations = new List<PromptOptimizationType> { optimizationType }
        };
    }

    public async Task<List<PromptVariant>> GenerateVariantsAsync(
        string originalPrompt,
        int count = 3,
        List<PromptOptimizationType>? types = null)
    {
        types ??= new List<PromptOptimizationType>
        {
            PromptOptimizationType.Clarity,
            PromptOptimizationType.Specificity,
            PromptOptimizationType.ChainOfThought
        };

        var variants = new List<PromptVariant>
        {
            new PromptVariant
            {
                Prompt = originalPrompt,
                Description = "原始提示词",
                AppliedOptimizations = new List<PromptOptimizationType>()
            }
        };

        foreach (var type in types.Take(count))
        {
            try
            {
                var variant = await OptimizePromptAsync(originalPrompt, type);
                variants.Add(variant);
            }
            catch { }
        }

        return variants;
    }

    public async Task<ABTestResult> RunABTestAsync(
        string prompt,
        string testInput,
        List<string>? variants = null,
        string? evaluationCriteria = null)
    {
        var result = new ABTestResult
        {
            OriginalPrompt = prompt
        };

        var testVariants = new List<PromptVariant>
        {
            new PromptVariant { Prompt = prompt, Description = "原始版本" }
        };

        if (variants != null)
        {
            for (int i = 0; i < variants.Count; i++)
            {
                testVariants.Add(new PromptVariant
                {
                    Prompt = variants[i],
                    Description = $"变体 {i + 1}"
                });
            }
        }
        else
        {
            var generatedVariants = await GenerateVariantsAsync(prompt);
            testVariants.AddRange(generatedVariants.Skip(1));
        }

        foreach (var variant in testVariants)
        {
            try
            {
                var fullPrompt = $"{variant.Prompt}\n\n输入：{testInput}";
                var response = await _aiService.SendMessageAsync(fullPrompt);

                variant.Score = await EvaluateResponseAsync(response.Content, evaluationCriteria);
                variant.Feedback = response.Content;

                result.Scores[variant.Id] = variant.Score ?? 0;
            }
            catch
            {
                variant.Score = 0;
                result.Scores[variant.Id] = 0;
            }
        }

        result.Variants = testVariants;
        result.WinningVariantId = testVariants.OrderByDescending(v => v.Score).First().Id;

        var winner = testVariants.First(v => v.Id == result.WinningVariantId);
        result.Conclusion = $"最佳版本: {winner.Description}，得分: {winner.Score:F2}";

        _testHistory.Add(result);
        OnABTestCompleted?.Invoke(result);

        return result;
    }

    private async Task<double> EvaluateResponseAsync(string response, string? criteria)
    {
        var evalPrompt = $@"请评估以下AI回复的质量，从0到10打分。

回复内容：
{response}

{(string.IsNullOrEmpty(criteria) ? "评估标准：准确性、完整性、清晰度" : $"评估标准：{criteria}")}

请只返回一个0到10之间的数字：";

        try
        {
            var evalResponse = await _aiService.SendMessageAsync(evalPrompt);
            var scoreStr = evalResponse.Content.Trim();

            if (double.TryParse(scoreStr, out var score))
            {
                return Math.Clamp(score / 10.0, 0, 1);
            }

            return 0.5;
        }
        catch
        {
            return 0.5;
        }
    }

    private string GetOptimizationInstruction(PromptOptimizationType type)
    {
        return type switch
        {
            PromptOptimizationType.Clarity => "使提示词更加清晰易懂，消除歧义",
            PromptOptimizationType.Specificity => "添加具体的细节、约束条件和期望输出格式",
            PromptOptimizationType.Structure => "优化提示词的结构，使用分段、编号等方式组织内容",
            PromptOptimizationType.ChainOfThought => "引导AI进行逐步思考，添加'让我们一步步思考'等引导语",
            PromptOptimizationType.RolePlay => "为AI设定一个专业角色，增强回答的针对性",
            PromptOptimizationType.OutputFormat => "明确指定输出的格式，如JSON、Markdown、列表等",
            PromptOptimizationType.Constraint => "添加明确的约束条件，如字数限制、必须包含的内容等",
            PromptOptimizationType.Examples => "添加示例来说明期望的输入输出格式",
            _ => "改进提示词的整体质量"
        };
    }

    private string GetOptimizationDescription(PromptOptimizationType type)
    {
        return type switch
        {
            PromptOptimizationType.Clarity => "清晰度优化",
            PromptOptimizationType.Specificity => "具体性优化",
            PromptOptimizationType.Structure => "结构优化",
            PromptOptimizationType.ChainOfThought => "思维链优化",
            PromptOptimizationType.RolePlay => "角色扮演优化",
            PromptOptimizationType.OutputFormat => "输出格式优化",
            PromptOptimizationType.Constraint => "约束条件优化",
            PromptOptimizationType.Examples => "示例优化",
            _ => "综合优化"
        };
    }

    public async Task<string> EnhanceWithChainOfThoughtAsync(string prompt)
    {
        var enhancedPrompt = $@"{prompt}

请按以下步骤思考：
1. 首先，理解问题的关键点
2. 然后，分析可能的解决方案
3. 接着，选择最佳方案
4. 最后，给出详细的回答

让我们一步步思考：";

        return enhancedPrompt;
    }

    public async Task<string> EnhanceWithRoleAsync(string prompt, string role)
    {
        var enhancedPrompt = $@"你是一位{role}。

{prompt}

请以{role}的专业视角回答：";

        return enhancedPrompt;
    }

    public async Task<string> EnhanceWithExamplesAsync(string prompt, List<(string input, string output)> examples)
    {
        var sb = new StringBuilder();
        sb.AppendLine(prompt);
        sb.AppendLine();
        sb.AppendLine("参考示例：");

        foreach (var (input, output) in examples)
        {
            sb.AppendLine($"输入：{input}");
            sb.AppendLine($"输出：{output}");
            sb.AppendLine();
        }

        sb.AppendLine("现在请处理：");

        return sb.ToString();
    }

    public List<ABTestResult> GetTestHistory()
    {
        return _testHistory.OrderByDescending(t => t.CreatedAt).ToList();
    }

    public string GeneratePromptTemplate(string category, string description)
    {
        return category.ToLower() switch
        {
            "code" => $@"作为一位资深程序员，请帮我完成以下任务：

任务描述：{description}

要求：
1. 代码需要清晰、高效、可维护
2. 添加必要的注释
3. 考虑边界情况和错误处理

请提供完整的代码实现：",

            "writing" => $@"作为一位专业写作顾问，请帮我完成以下任务：

任务描述：{description}

要求：
1. 语言流畅、逻辑清晰
2. 适合目标读者群体
3. 突出核心观点

请提供你的写作内容：",

            "analysis" => $@"作为一位数据分析师，请帮我分析以下内容：

分析对象：{description}

请从以下角度进行分析：
1. 核心要点
2. 关键数据
3. 潜在问题
4. 改进建议

请提供详细的分析报告：",

            _ => $@"请帮我完成以下任务：

{description}

请提供详细的回答："
        };
    }
}

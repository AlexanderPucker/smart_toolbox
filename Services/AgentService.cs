using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public enum AgentState
{
    Idle,
    Planning,
    Executing,
    Reflecting,
    Completed,
    Failed
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class AgentTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public AgentState State { get; set; } = AgentState.Idle;
    public List<SubTask> SubTasks { get; set; } = new();
    public List<string> ToolsAvailable { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public List<string> ExecutionLog { get; set; } = new();
    public string? Result { get; set; }
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
}

public class SubTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
}

public class AgentMemory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "episodic";
    public string Content { get; set; } = string.Empty;
    public double Importance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastAccessed { get; set; }
    public int AccessCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ReflectionResult
{
    public bool Success { get; set; }
    public double Confidence { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public string? NextAction { get; set; }
    public string? Summary { get; set; }
}

public sealed class AgentService
{
    private static readonly Lazy<AgentService> _instance = new(() => new AgentService());
    public static AgentService Instance => _instance.Value;

    private readonly AIService _aiService;
    private readonly ToolRegistry _toolRegistry;
    private readonly List<AgentMemory> _memories = new();
    private readonly List<AgentTask> _taskHistory = new();
    private AgentTask? _currentTask;

    public event Action<AgentTask>? OnTaskCreated;
    public event Action<AgentTask, SubTask>? OnSubTaskStarted;
    public event Action<AgentTask, SubTask>? OnSubTaskCompleted;
    public event Action<AgentTask, ReflectionResult>? OnReflectionCompleted;
    public event Action<AgentTask>? OnTaskCompleted;

    private readonly int _maxMemoryItems = 1000;
    private readonly double _importanceThreshold = 0.5;

    private AgentService()
    {
        _aiService = AIService.Instance;
        _toolRegistry = ToolRegistry.Instance;
    }

    public async Task<AgentTask> CreateTaskAsync(string description, TaskPriority priority = TaskPriority.Medium)
    {
        var task = new AgentTask
        {
            Description = description,
            Priority = priority,
            State = AgentState.Planning
        };

        _currentTask = task;
        OnTaskCreated?.Invoke(task);

        await PlanTaskAsync(task);

        return task;
    }

    private async Task PlanTaskAsync(AgentTask task)
    {
        task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 开始规划任务: {task.Description}");

        var relevantMemories = RetrieveRelevantMemories(task.Description);
        var memoryContext = relevantMemories.Any()
            ? $"\n\n相关历史经验:\n{string.Join("\n", relevantMemories.Take(5).Select(m => $"- {m.Content}"))}"
            : "";

        var planningPrompt = $@"你是一个智能任务规划代理。请分析以下任务并分解为具体的执行步骤。

任务: {task.Description}
{memoryContext}

请按以下格式输出规划结果:
1. 分析任务目标
2. 列出需要的步骤（每个步骤应该是一个具体的、可执行的动作）
3. 识别可能需要的工具
4. 评估任务的复杂度和风险

输出格式（JSON）:
{{
    ""goal"": ""任务目标"",
    ""complexity"": ""low/medium/high"",
    ""risks"": [""风险1"", ""风险2""],
    ""steps"": [
        {{""description"": ""步骤描述"", ""action"": ""具体动作"", ""tool"": ""工具名称""}}
    ]
}}";

        var response = await _aiService.SendMessageAsync(planningPrompt, "你是一个专业的任务规划专家。");

        if (response.IsSuccess)
        {
            try
            {
                var plan = ParsePlanResponse(response.Content);
                task.Goal = plan.Goal;
                
                foreach (var step in plan.Steps)
                {
                    task.SubTasks.Add(new SubTask
                    {
                        Description = step.Description,
                        Action = step.Action,
                        MaxRetries = 3
                    });
                }

                task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 规划完成，共 {task.SubTasks.Count} 个步骤");
            }
            catch (Exception ex)
            {
                task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 规划解析失败: {ex.Message}");
                task.State = AgentState.Failed;
            }
        }
        else
        {
            task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 规划失败: {response.ErrorMessage}");
            task.State = AgentState.Failed;
        }
    }

    private (string Goal, List<(string Description, string Action)> Steps) ParsePlanResponse(string content)
    {
        var goal = "完成用户任务";
        var steps = new List<(string, string)>();

        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var plan = JsonSerializer.Deserialize<JsonElement>(json);

                if (plan.TryGetProperty("goal", out var goalProp))
                {
                    goal = goalProp.GetString() ?? goal;
                }

                if (plan.TryGetProperty("steps", out var stepsProp))
                {
                    foreach (var step in stepsProp.EnumerateArray())
                    {
                        var desc = step.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                        var action = step.TryGetProperty("action", out var a) ? a.GetString() ?? "" : "";
                        steps.Add((desc, action));
                    }
                }
            }
        }
        catch { }

        if (!steps.Any())
        {
            steps.Add(("执行任务", "完成用户请求的任务"));
        }

        return (goal, steps);
    }

    public async Task<AgentTask> ExecuteTaskAsync(AgentTask? task = null, CancellationToken cancellationToken = default)
    {
        task ??= _currentTask;

        if (task == null)
        {
            throw new InvalidOperationException("没有可执行的任务");
        }

        task.State = AgentState.Executing;
        task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 开始执行任务");

        foreach (var subTask in task.SubTasks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                task.State = AgentState.Failed;
                task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 任务被取消");
                break;
            }

            OnSubTaskStarted?.Invoke(task, subTask);
            task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 执行子任务: {subTask.Description}");

            var success = await ExecuteSubTaskAsync(task, subTask, cancellationToken);

            if (!success && !subTask.IsCompleted)
            {
                for (int retry = 0; retry < subTask.MaxRetries && !success; retry++)
                {
                    subTask.RetryCount++;
                    task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 重试子任务 ({retry + 1}/{subTask.MaxRetries})");
                    success = await ExecuteSubTaskAsync(task, subTask, cancellationToken);
                }
            }

            OnSubTaskCompleted?.Invoke(task, subTask);

            if (!success)
            {
                task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 子任务失败: {subTask.Error}");
            }
        }

        task.State = task.SubTasks.All(s => s.IsSuccessful) ? AgentState.Completed : AgentState.Failed;
        task.CompletedAt = DateTime.Now;
        task.Duration = task.CompletedAt - task.CreatedAt;

        if (task.State == AgentState.Completed)
        {
            task.Result = task.SubTasks.LastOrDefault()?.Result ?? "任务完成";
        }

        _taskHistory.Add(task);
        AddMemory(new AgentMemory
        {
            Type = "episodic",
            Content = $"任务: {task.Description}\n结果: {task.Result}",
            Importance = task.State == AgentState.Completed ? 0.8 : 0.5,
            Tags = new List<string> { "task", task.State.ToString() }
        });

        OnTaskCompleted?.Invoke(task);
        return task;
    }

    private async Task<bool> ExecuteSubTaskAsync(AgentTask task, SubTask subTask, CancellationToken cancellationToken)
    {
        try
        {
            var actionPrompt = $@"你是一个任务执行代理。请执行以下动作:

任务上下文: {task.Description}
当前步骤: {subTask.Description}
具体动作: {subTask.Action}

之前的执行结果:
{string.Join("\n", task.SubTasks.Where(s => s.IsCompleted).Select(s => $"- {s.Description}: {s.Result}"))}

请输出执行结果。如果需要使用工具，请说明工具名称和参数。";

            var response = await _aiService.SendMessageAsync(actionPrompt, "你是一个高效的任务执行者。", cancellationToken: cancellationToken);

            if (response.IsSuccess)
            {
                subTask.Result = response.Content;
                subTask.IsCompleted = true;
                subTask.IsSuccessful = true;
                return true;
            }
            else
            {
                subTask.Error = response.ErrorMessage;
                subTask.IsCompleted = true;
                subTask.IsSuccessful = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            subTask.Error = ex.Message;
            subTask.IsCompleted = true;
            subTask.IsSuccessful = false;
            return false;
        }
    }

    public async Task<ReflectionResult> ReflectAsync(AgentTask task)
    {
        task.State = AgentState.Reflecting;
        task.ExecutionLog.Add($"[{DateTime.Now:HH:mm:ss}] 开始反思");

        var reflectionPrompt = $@"请反思以下任务的执行过程:

任务: {task.Description}
目标: {task.Goal}

执行步骤:
{string.Join("\n", task.SubTasks.Select((s, i) => $"{i + 1}. {s.Description}: {(s.IsSuccessful ? "成功" : "失败")} - {s.Result ?? s.Error}"))}

请评估:
1. 任务是否成功完成？
2. 有哪些问题？
3. 可以如何改进？
4. 对结果的置信度（0-1）

输出JSON格式:
{{
    ""success"": true/false,
    ""confidence"": 0.0-1.0,
    ""issues"": [""问题1""],
    ""improvements"": [""改进建议1""],
    ""summary"": ""总结""
}}";

        var response = await _aiService.SendMessageAsync(reflectionPrompt, "你是一个善于反思的AI代理。");

        var result = new ReflectionResult();

        if (response.IsSuccess)
        {
            try
            {
                var jsonStart = response.Content.IndexOf('{');
                var jsonEnd = response.Content.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = response.Content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var reflection = JsonSerializer.Deserialize<JsonElement>(json);

                    result.Success = reflection.TryGetProperty("success", out var s) && s.GetBoolean();
                    result.Confidence = reflection.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.5;
                    result.Summary = reflection.TryGetProperty("summary", out var sum) ? sum.GetString() : "";

                    if (reflection.TryGetProperty("issues", out var issues))
                    {
                        foreach (var issue in issues.EnumerateArray())
                        {
                            result.Issues.Add(issue.GetString() ?? "");
                        }
                    }

                    if (reflection.TryGetProperty("improvements", out var improvements))
                    {
                        foreach (var imp in improvements.EnumerateArray())
                        {
                            result.Improvements.Add(imp.GetString() ?? "");
                        }
                    }
                }
            }
            catch { }

            task.Confidence = result.Confidence;
        }

        OnReflectionCompleted?.Invoke(task, result);
        return result;
    }

    public void AddMemory(AgentMemory memory)
    {
        _memories.Add(memory);

        if (_memories.Count > _maxMemoryItems)
        {
            var toRemove = _memories
                .OrderBy(m => m.Importance)
                .ThenBy(m => m.LastAccessed ?? m.CreatedAt)
                .Take(_memories.Count - _maxMemoryItems)
                .ToList();

            foreach (var m in toRemove)
            {
                _memories.Remove(m);
            }
        }
    }

    public List<AgentMemory> RetrieveRelevantMemories(string query, int topK = 10)
    {
        var queryKeywords = query.ToLower().Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        return _memories
            .Select(m => new
            {
                Memory = m,
                Score = CalculateRelevanceScore(m, queryKeywords, query)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x =>
            {
                x.Memory.LastAccessed = DateTime.Now;
                x.Memory.AccessCount++;
                return x.Memory;
            })
            .ToList();
    }

    private double CalculateRelevanceScore(AgentMemory memory, string[] queryKeywords, string originalQuery)
    {
        double score = 0;
        var contentLower = memory.Content.ToLower();

        foreach (var keyword in queryKeywords)
        {
            if (contentLower.Contains(keyword))
            {
                score += 1.0;
            }
        }

        foreach (var tag in memory.Tags)
        {
            if (queryKeywords.Any(k => tag.ToLower().Contains(k)))
            {
                score += 0.5;
            }
        }

        score += memory.Importance * 0.3;
        score += Math.Min(memory.AccessCount * 0.1, 1.0);

        var recency = (DateTime.Now - memory.CreatedAt).TotalDays;
        score += Math.Max(0, 1 - recency / 30) * 0.2;

        return score;
    }

    public List<AgentTask> GetTaskHistory(int count = 50)
    {
        return _taskHistory
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToList();
    }

    public AgentTask? GetCurrentTask()
    {
        return _currentTask;
    }

    public void ClearMemories()
    {
        _memories.Clear();
    }

    public void ClearHistory()
    {
        _taskHistory.Clear();
    }

    public async Task<string> AutoImproveAsync(string previousTask, string previousResult, string feedback)
    {
        var improvePrompt = $@"基于之前的任务执行结果和用户反馈，请改进执行方案。

之前的任务: {previousTask}
执行结果: {previousResult}
用户反馈: {feedback}

请输出改进后的执行方案:";

        var response = await _aiService.SendMessageAsync(improvePrompt, "你是一个持续学习的AI代理。");
        return response.Content;
    }

    public Dictionary<string, object> GetStatistics()
    {
        var completed = _taskHistory.Count(t => t.State == AgentState.Completed);
        var failed = _taskHistory.Count(t => t.State == AgentState.Failed);

        return new Dictionary<string, object>
        {
            { "TotalTasks", _taskHistory.Count },
            { "CompletedTasks", completed },
            { "FailedTasks", failed },
            { "SuccessRate", _taskHistory.Count > 0 ? (double)completed / _taskHistory.Count : 0 },
            { "TotalMemories", _memories.Count },
            { "AverageSubTasks", _taskHistory.Count > 0 ? _taskHistory.Average(t => t.SubTasks.Count) : 0 }
        };
    }
}

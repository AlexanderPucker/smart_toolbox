using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public class WorkflowNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "ai_task";
    public string? ToolName { get; set; }
    public string? PromptTemplate { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, string> Inputs { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 60;
    public int RetryCount { get; set; } = 0;
    public bool ContinueOnError { get; set; }
}

public class Workflow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkflowNode> Nodes { get; set; } = new();
    public Dictionary<string, string> Variables { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class WorkflowExecutionResult
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
    public int TokensUsed { get; set; }
    public double Cost { get; set; }
}

public class WorkflowRunResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<WorkflowExecutionResult> NodeResults { get; set; } = new();
    public string FinalOutput { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class WorkflowEngine
{
    private static readonly Lazy<WorkflowEngine> _instance = new(() => new WorkflowEngine());
    public static WorkflowEngine Instance => _instance.Value;

    private readonly Dictionary<string, Workflow> _workflows = new();
    private readonly AIService _aiService;
    private readonly ToolRegistry _toolRegistry;
    private readonly TokenCounterService _tokenCounter;

    public event Action<Workflow>? OnWorkflowCreated;
    public event Action<string, WorkflowRunResult>? OnWorkflowCompleted;
    public event Action<string, WorkflowExecutionResult>? OnNodeCompleted;

    private WorkflowEngine()
    {
        _aiService = AIService.Instance;
        _toolRegistry = ToolRegistry.Instance;
        _tokenCounter = TokenCounterService.Instance;
        InitializeDefaultWorkflows();
    }

    private void InitializeDefaultWorkflows()
    {
        var codeReviewWorkflow = new Workflow
        {
            Id = "code-review",
            Name = "代码审查流水线",
            Description = "自动分析代码、生成文档和测试建议"
        };

        codeReviewWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "explain",
            Name = "代码解释",
            Type = "ai_task",
            PromptTemplate = "请解释以下代码的功能和逻辑：\n\n{code}",
            Model = "gpt-4o"
        });

        codeReviewWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "review",
            Name = "代码审查",
            Type = "ai_task",
            PromptTemplate = "请审查以下代码，指出潜在问题和改进建议：\n\n{code}",
            Model = "gpt-4o",
            Dependencies = new List<string> { "explain" }
        });

        codeReviewWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "summary",
            Name = "生成摘要",
            Type = "ai_task",
            PromptTemplate = "基于以下分析，生成简洁的代码摘要：\n\n解释：{explain.output}\n\n审查：{review.output}",
            Model = "gpt-4o-mini",
            Dependencies = new List<string> { "explain", "review" }
        });

        _workflows[codeReviewWorkflow.Id] = codeReviewWorkflow;

        var translationWorkflow = new Workflow
        {
            Id = "translation-pipeline",
            Name = "翻译流水线",
            Description = "翻译、校对和润色文本"
        };

        translationWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "translate",
            Name = "翻译",
            Type = "ai_task",
            PromptTemplate = "请将以下文本翻译为{target_language}：\n\n{text}",
            Model = "gpt-4o-mini"
        });

        translationWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "polish",
            Name = "润色",
            Type = "ai_task",
            PromptTemplate = "请润色以下翻译结果，使其更自然流畅：\n\n{translate.output}",
            Model = "gpt-4o-mini",
            Dependencies = new List<string> { "translate" }
        });

        _workflows[translationWorkflow.Id] = translationWorkflow;

        var documentAnalysisWorkflow = new Workflow
        {
            Id = "document-analysis",
            Name = "文档分析流水线",
            Description = "提取关键信息、生成摘要和问答"
        };

        documentAnalysisWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "extract",
            Name = "提取关键信息",
            Type = "ai_task",
            PromptTemplate = "请从以下文档中提取关键信息和要点：\n\n{document}",
            Model = "gpt-4o"
        });

        documentAnalysisWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "summarize",
            Name = "生成摘要",
            Type = "ai_task",
            PromptTemplate = "请基于以下关键信息生成文档摘要：\n\n{extract.output}",
            Model = "gpt-4o-mini",
            Dependencies = new List<string> { "extract" }
        });

        documentAnalysisWorkflow.Nodes.Add(new WorkflowNode
        {
            Id = "questions",
            Name = "生成问答",
            Type = "ai_task",
            PromptTemplate = "请基于以下文档内容生成5个常见问题及其答案：\n\n文档：{document}\n\n摘要：{summarize.output}",
            Model = "gpt-4o-mini",
            Dependencies = new List<string> { "extract", "summarize" }
        });

        _workflows[documentAnalysisWorkflow.Id] = documentAnalysisWorkflow;
    }

    public Workflow CreateWorkflow(string name, string description = "")
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description
        };

        _workflows[workflow.Id] = workflow;
        OnWorkflowCreated?.Invoke(workflow);
        return workflow;
    }

    public void AddNodeToWorkflow(string workflowId, WorkflowNode node)
    {
        if (_workflows.TryGetValue(workflowId, out var workflow))
        {
            workflow.Nodes.Add(node);
            workflow.UpdatedAt = DateTime.Now;
        }
    }

    public void RemoveNodeFromWorkflow(string workflowId, string nodeId)
    {
        if (_workflows.TryGetValue(workflowId, out var workflow))
        {
            workflow.Nodes.RemoveAll(n => n.Id == nodeId);
            workflow.UpdatedAt = DateTime.Now;
        }
    }

    public Workflow? GetWorkflow(string workflowId)
    {
        return _workflows.GetValueOrDefault(workflowId);
    }

    public List<Workflow> GetAllWorkflows()
    {
        return _workflows.Values.ToList();
    }

    public void DeleteWorkflow(string workflowId)
    {
        _workflows.Remove(workflowId);
    }

    public async Task<WorkflowRunResult> RunWorkflowAsync(
        string workflowId,
        Dictionary<string, string> inputs,
        System.Threading.CancellationToken cancellationToken = default)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
        {
            return new WorkflowRunResult
            {
                WorkflowId = workflowId,
                Success = false,
                ErrorMessage = $"未找到工作流: {workflowId}"
            };
        }

        var startTime = DateTime.Now;
        var result = new WorkflowRunResult { WorkflowId = workflowId };
        var nodeOutputs = new Dictionary<string, string>();
        var variables = new Dictionary<string, string>(workflow.Variables);

        foreach (var input in inputs)
        {
            variables[input.Key] = input.Value;
        }

        var sortedNodes = TopologicalSort(workflow.Nodes);

        foreach (var node in sortedNodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.Success = false;
                result.ErrorMessage = "工作流被取消";
                break;
            }

            var nodeResult = await ExecuteNodeAsync(node, variables, nodeOutputs, cancellationToken);
            result.NodeResults.Add(nodeResult);

            if (nodeResult.Success)
            {
                nodeOutputs[node.Id] = nodeResult.Output;
                result.TotalTokens += nodeResult.TokensUsed;
                result.TotalCost += nodeResult.Cost;
            }
            else if (!node.ContinueOnError)
            {
                result.Success = false;
                result.ErrorMessage = $"节点 '{node.Name}' 执行失败: {nodeResult.Error}";
                break;
            }

            OnNodeCompleted?.Invoke(workflowId, nodeResult);
        }

        result.TotalDuration = DateTime.Now - startTime;
        result.Success = result.NodeResults.All(r => r.Success);
        result.FinalOutput = nodeOutputs.Values.LastOrDefault() ?? string.Empty;

        OnWorkflowCompleted?.Invoke(workflowId, result);
        return result;
    }

    private async Task<WorkflowExecutionResult> ExecuteNodeAsync(
        WorkflowNode node,
        Dictionary<string, string> variables,
        Dictionary<string, string> nodeOutputs,
        System.Threading.CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;
        var result = new WorkflowExecutionResult
        {
            NodeId = node.Id,
            NodeName = node.Name
        };

        try
        {
            var prompt = ResolveTemplate(node.PromptTemplate ?? "", variables, nodeOutputs);

            if (node.Type == "tool" && !string.IsNullOrEmpty(node.ToolName))
            {
                result.Output = await _toolRegistry.ExecuteToolAsync(node.ToolName, prompt);
            }
            else
            {
                var aiResponse = await _aiService.SendMessageAsync(prompt, "", node.Model, cancellationToken);

                if (aiResponse.IsSuccess)
                {
                    result.Output = aiResponse.Content;
                    result.TokensUsed = aiResponse.TotalTokens;
                    result.Cost = aiResponse.EstimatedCost;
                    result.Success = true;
                }
                else
                {
                    result.Error = aiResponse.ErrorMessage;
                    result.Success = false;
                }
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.Success = false;
        }

        result.Duration = DateTime.Now - startTime;
        return result;
    }

    private string ResolveTemplate(
        string template,
        Dictionary<string, string> variables,
        Dictionary<string, string> nodeOutputs)
    {
        var result = template;

        foreach (var variable in variables)
        {
            result = result.Replace($"{{{variable.Key}}}", variable.Value);
        }

        foreach (var output in nodeOutputs)
        {
            result = result.Replace($"{{{output.Key}.output}}", output.Value);
        }

        return result;
    }

    private List<WorkflowNode> TopologicalSort(List<WorkflowNode> nodes)
    {
        var result = new List<WorkflowNode>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(WorkflowNode node)
        {
            if (visited.Contains(node.Id))
                return;

            if (visiting.Contains(node.Id))
                throw new InvalidOperationException("检测到循环依赖");

            visiting.Add(node.Id);

            foreach (var depId in node.Dependencies)
            {
                var dep = nodes.FirstOrDefault(n => n.Id == depId);
                if (dep != null)
                {
                    Visit(dep);
                }
            }

            visiting.Remove(node.Id);
            visited.Add(node.Id);
            result.Add(node);
        }

        foreach (var node in nodes)
        {
            Visit(node);
        }

        return result;
    }

    public Workflow CreateCustomWorkflow(string name, List<WorkflowNode> nodes, Dictionary<string, string> variables)
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Nodes = nodes,
            Variables = variables
        };

        _workflows[workflow.Id] = workflow;
        OnWorkflowCreated?.Invoke(workflow);
        return workflow;
    }

    public bool ValidateWorkflow(Workflow workflow, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrEmpty(workflow.Name))
        {
            errors.Add("工作流名称不能为空");
        }

        if (workflow.Nodes.Count == 0)
        {
            errors.Add("工作流至少需要一个节点");
        }

        var nodeIds = workflow.Nodes.Select(n => n.Id).ToHashSet();

        foreach (var node in workflow.Nodes)
        {
            foreach (var depId in node.Dependencies)
            {
                if (!nodeIds.Contains(depId))
                {
                    errors.Add($"节点 '{node.Name}' 引用了不存在的依赖 '{depId}'");
                }
            }
        }

        try
        {
            TopologicalSort(workflow.Nodes);
        }
        catch (InvalidOperationException)
        {
            errors.Add("工作流存在循环依赖");
        }

        return errors.Count == 0;
    }
}

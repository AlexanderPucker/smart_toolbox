using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class WorkflowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private WorkflowItem? _selectedWorkflow;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _currentNode = string.Empty;

    public ObservableCollection<WorkflowItem> Workflows { get; } = new();
    public ObservableCollection<WorkflowNodeItem> Nodes { get; } = new();
    public ObservableCollection<WorkflowResultItem> Results { get; } = new();

    private readonly WorkflowEngine _workflowEngine;

    public WorkflowViewModel()
    {
        _workflowEngine = WorkflowEngine.Instance;
        _workflowEngine.OnWorkflowCreated += OnWorkflowCreated;
        _workflowEngine.OnNodeCompleted += OnNodeCompleted;
        LoadWorkflows();
    }

    private void OnWorkflowCreated(Workflow workflow)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Workflows.Add(new WorkflowItem
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                NodeCount = workflow.Nodes.Count
            });
        });
    }

    private void OnNodeCompleted(string workflowId, WorkflowExecutionResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Results.Add(new WorkflowResultItem
            {
                NodeName = result.NodeName,
                Success = result.Success,
                Output = result.Output,
                Duration = result.Duration.TotalSeconds,
                TokensUsed = result.TokensUsed,
                Cost = result.Cost
            });

            CurrentNode = result.NodeName;
            Progress = (double)Results.Count / Nodes.Count * 100;
        });
    }

    private void LoadWorkflows()
    {
        var workflows = _workflowEngine.GetAllWorkflows();
        Workflows.Clear();

        foreach (var workflow in workflows)
        {
            Workflows.Add(new WorkflowItem
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description,
                NodeCount = workflow.Nodes.Count
            });
        }

        StatusMessage = $"已加载 {Workflows.Count} 个工作流";
    }

    partial void OnSelectedWorkflowChanged(WorkflowItem? value)
    {
        if (value != null)
        {
            LoadWorkflowNodes(value.Id);
        }
    }

    private void LoadWorkflowNodes(string workflowId)
    {
        var workflow = _workflowEngine.GetWorkflow(workflowId);
        Nodes.Clear();

        if (workflow != null)
        {
            foreach (var node in workflow.Nodes)
            {
                Nodes.Add(new WorkflowNodeItem
                {
                    Id = node.Id,
                    Name = node.Name,
                    Type = node.Type,
                    ToolName = node.ToolName ?? "",
                    Dependencies = string.Join(", ", node.Dependencies)
                });
            }
        }
    }

    [RelayCommand]
    private async Task RunWorkflowAsync()
    {
        if (SelectedWorkflow == null)
        {
            StatusMessage = "请先选择一个工作流";
            return;
        }

        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusMessage = "请输入内容";
            return;
        }

        IsRunning = true;
        IsLoading = true;
        Progress = 0;
        Results.Clear();
        OutputText = string.Empty;
        StatusMessage = "正在运行工作流...";

        try
        {
            var inputs = new System.Collections.Generic.Dictionary<string, string>
            {
                { "text", InputText },
                { "code", InputText },
                { "document", InputText },
                { "target_language", "英语" }
            };

            var result = await _workflowEngine.RunWorkflowAsync(SelectedWorkflow.Id, inputs);

            OutputText = result.FinalOutput;
            Progress = 100;

            if (result.Success)
            {
                StatusMessage = $"工作流完成 - 耗时 {result.TotalDuration.TotalSeconds:F1}s, " +
                               $"Token: {result.TotalTokens}, 费用: ${result.TotalCost:F4}";
            }
            else
            {
                StatusMessage = $"工作流失败: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"运行失败: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void StopWorkflow()
    {
        StatusMessage = "工作流已停止";
        IsRunning = false;
    }

    [RelayCommand]
    private void ClearResults()
    {
        Results.Clear();
        OutputText = string.Empty;
        Progress = 0;
        StatusMessage = "已清空结果";
    }

    [RelayCommand]
    private void CreateWorkflow()
    {
        var workflow = _workflowEngine.CreateWorkflow("新工作流", "自定义工作流");
        StatusMessage = "已创建新工作流";
    }
}

public class WorkflowItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int NodeCount { get; set; }
}

public class WorkflowNodeItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string Dependencies { get; set; } = string.Empty;
}

public class WorkflowResultItem
{
    public string NodeName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public double Duration { get; set; }
    public int TokensUsed { get; set; }
    public double Cost { get; set; }
}

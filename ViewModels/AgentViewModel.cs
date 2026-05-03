using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AgentViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _taskInput = string.Empty;

    [ObservableProperty]
    private string _taskResult = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _currentStep = string.Empty;

    [ObservableProperty]
    private AgentTaskItem? _selectedTask;

    [ObservableProperty]
    private TaskPriority _selectedPriority = TaskPriority.Medium;

    public ObservableCollection<AgentTaskItem> TaskHistory { get; } = new();
    public ObservableCollection<SubTaskItem> CurrentSubTasks { get; } = new();
    public ObservableCollection<string> ExecutionLog { get; } = new();

    private readonly AgentService _agentService;

    public AgentViewModel()
    {
        _agentService = AgentService.Instance;
        _agentService.OnTaskCreated += OnTaskCreated;
        _agentService.OnSubTaskStarted += OnSubTaskStarted;
        _agentService.OnSubTaskCompleted += OnSubTaskCompleted;
        _agentService.OnTaskCompleted += OnTaskCompleted;

        LoadHistory();
    }

    private void OnTaskCreated(AgentTask task)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = "任务已创建，正在规划...";
        });
    }

    private void OnSubTaskStarted(AgentTask task, SubTask subTask)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            CurrentStep = $"执行: {subTask.Description}";
            ExecutionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] 开始: {subTask.Description}");
        });
    }

    private void OnSubTaskCompleted(AgentTask task, SubTask subTask)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var status = subTask.IsSuccessful ? "✓" : "✗";
            ExecutionLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {status} {subTask.Description}");

            var item = CurrentSubTasks.FirstOrDefault(s => s.Id == subTask.Id);
            if (item != null)
            {
                item.IsCompleted = true;
                item.IsSuccessful = subTask.IsSuccessful;
                item.Result = subTask.Result ?? subTask.Error;
            }

            Progress = (double)task.SubTasks.Count(s => s.IsCompleted) / task.SubTasks.Count * 100;
        });
    }

    private void OnTaskCompleted(AgentTask task)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            IsExecuting = false;
            Progress = 100;

            if (task.State == Services.AgentState.Completed)
            {
                TaskResult = task.Result ?? "任务完成";
                StatusMessage = "任务执行成功";
            }
            else
            {
                StatusMessage = $"任务失败: {task.SubTasks.FirstOrDefault(s => !s.IsSuccessful)?.Error}";
            }

            TaskHistory.Insert(0, new AgentTaskItem
            {
                Id = task.Id,
                Description = task.Description,
                State = task.State.ToString(),
                CreatedAt = task.CreatedAt,
                Duration = task.Duration?.TotalSeconds ?? 0,
                Confidence = task.Confidence
            });

            await Task.Delay(500);
            Progress = 0;
        });
    }

    private void LoadHistory()
    {
        var history = _agentService.GetTaskHistory(20);
        foreach (var task in history)
        {
            TaskHistory.Add(new AgentTaskItem
            {
                Id = task.Id,
                Description = task.Description,
                State = task.State.ToString(),
                CreatedAt = task.CreatedAt,
                Duration = task.Duration?.TotalSeconds ?? 0,
                Confidence = task.Confidence
            });
        }
    }

    [RelayCommand]
    private async Task CreateAndExecuteTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(TaskInput))
        {
            StatusMessage = "请输入任务描述";
            return;
        }

        IsExecuting = true;
        Progress = 0;
        TaskResult = string.Empty;
        ExecutionLog.Clear();
        CurrentSubTasks.Clear();

        try
        {
            var task = await _agentService.CreateTaskAsync(TaskInput, SelectedPriority);

            foreach (var subTask in task.SubTasks)
            {
                CurrentSubTasks.Add(new SubTaskItem
                {
                    Id = subTask.Id,
                    Description = subTask.Description,
                    IsCompleted = false,
                    IsSuccessful = false
                });
            }

            await _agentService.ExecuteTaskAsync(task);
        }
        catch (Exception ex)
        {
            StatusMessage = $"执行失败: {ex.Message}";
            IsExecuting = false;
        }
    }

    [RelayCommand]
    private async Task ReflectOnTaskAsync()
    {
        if (_agentService.GetCurrentTask() == null)
        {
            StatusMessage = "没有可反思的任务";
            return;
        }

        StatusMessage = "正在反思任务执行...";

        var result = await _agentService.ReflectAsync(_agentService.GetCurrentTask());

        if (result.Success)
        {
            TaskResult = $"反思结果:\n\n置信度: {result.Confidence:P}\n\n{result.Summary}";
            StatusMessage = "反思完成";
        }
        else
        {
            StatusMessage = "反思失败";
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _agentService.ClearHistory();
        TaskHistory.Clear();
        StatusMessage = "历史已清空";
    }

    [RelayCommand]
    private void ClearLog()
    {
        ExecutionLog.Clear();
    }
}

public class AgentTaskItem
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public double Duration { get; set; }
    public double Confidence { get; set; }
}

public class SubTaskItem
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Result { get; set; }
}

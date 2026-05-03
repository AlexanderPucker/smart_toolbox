using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Models;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AIChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "输入消息开始对话";

    [ObservableProperty]
    private string _systemPrompt = "你是一个友好的AI助手";

    [ObservableProperty]
    private string _selectedModel = "gpt-3.5-turbo";

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _useStreaming = true;

    [ObservableProperty]
    private ConversationItem? _selectedConversation;

    [ObservableProperty]
    private int _totalTokens;

    [ObservableProperty]
    private double _totalCost;

    [ObservableProperty]
    private bool _enableTools;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<ConversationItem> Conversations { get; } = new();
    public ObservableCollection<string> AvailableModels { get; } = new();
    public ObservableCollection<ContextInfoItem> ContextInfo { get; } = new();

    private readonly AIService _aiService;
    private readonly ConversationManager _conversationManager;
    private readonly TokenCounterService _tokenCounter;
    private readonly ContextWindowManager _contextManager;
    private readonly ToolRegistry _toolRegistry;
    private CancellationTokenSource? _streamingCts;

    public AIChatViewModel()
    {
        _aiService = AIService.Instance;
        _conversationManager = ConversationManager.Instance;
        _tokenCounter = TokenCounterService.Instance;
        _contextManager = ContextWindowManager.Instance;
        _toolRegistry = ToolRegistry.Instance;

        _aiService.OnStreamingChunk += OnStreamingChunk;
        _aiService.OnResponseReceived += OnResponseReceived;
        _conversationManager.OnConversationCreated += OnConversationCreated;
        _conversationManager.OnConversationUpdated += OnConversationUpdated;
        _tokenCounter.OnUsageRecorded += OnUsageRecorded;

        LoadAvailableModels();
        LoadConversations();
        CreateNewConversation();
    }

    private void OnStreamingChunk(string chunk)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (Messages.Count > 0)
            {
                var lastMessage = Messages.Last();
                if (lastMessage.Role == "assistant" && lastMessage.IsStreaming)
                {
                    lastMessage.Content += chunk;
                }
            }
        });
    }

    private void OnResponseReceived(AIResponse response)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TotalTokens += response.TotalTokens;
            TotalCost += response.EstimatedCost;

            if (_selectedConversation != null)
            {
                _conversationManager.UpdateConversationStats(
                    _selectedConversation.Id,
                    response.TotalTokens,
                    response.EstimatedCost
                );
            }
        });
    }

    private void OnConversationCreated(Conversation conversation)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Conversations.Insert(0, new ConversationItem
            {
                Id = conversation.Id,
                Title = conversation.Title,
                UpdatedAt = conversation.UpdatedAt,
                MessageCount = conversation.Messages.Count
            });
        });
    }

    private void OnConversationUpdated(Conversation conversation)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var item = Conversations.FirstOrDefault(c => c.Id == conversation.Id);
            if (item != null)
            {
                item.Title = conversation.Title;
                item.UpdatedAt = conversation.UpdatedAt;
                item.MessageCount = conversation.Messages.Count;
            }
        });
    }

    private void OnUsageRecorded(UsageRecord record)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateContextInfo());
    }

    private void LoadAvailableModels()
    {
        var models = _aiService.GetAvailableModels();
        AvailableModels.Clear();

        foreach (var model in models)
        {
            AvailableModels.Add(model.Id);
        }

        if (AvailableModels.Count > 0 && !AvailableModels.Contains(SelectedModel))
        {
            SelectedModel = AvailableModels[0];
        }
    }

    private void LoadConversations()
    {
        var conversations = _conversationManager.GetAllConversations();
        Conversations.Clear();

        foreach (var conv in conversations)
        {
            Conversations.Add(new ConversationItem
            {
                Id = conv.Id,
                Title = conv.Title,
                UpdatedAt = conv.UpdatedAt,
                MessageCount = conv.Messages.Count
            });
        }
    }

    [RelayCommand]
    private void CreateNewConversation()
    {
        var conversation = _conversationManager.CreateConversation("新对话", SystemPrompt);
        SelectedConversation = Conversations.FirstOrDefault(c => c.Id == conversation.Id);
        Messages.Clear();
        TotalTokens = 0;
        TotalCost = 0;
        StatusMessage = "新对话已创建";
    }

    partial void OnSelectedConversationChanged(ConversationItem? value)
    {
        if (value != null)
        {
            _conversationManager.SetCurrentConversation(value.Id);
            LoadConversationMessages(value.Id);
        }
    }

    private void LoadConversationMessages(Guid conversationId)
    {
        var conversation = _conversationManager.GetConversation(conversationId.ToString());
        if (conversation == null) return;

        Messages.Clear();
        foreach (var msg in conversation.Messages)
        {
            Messages.Add(new ChatMessage(msg.Role, msg.Content)
            {
                Timestamp = msg.Timestamp,
                IsPinned = msg.IsPinned,
                TokenCount = msg.TokenCount
            });
        }

        TotalTokens = conversation.TotalTokens;
        TotalCost = conversation.TotalCost;
        SystemPrompt = conversation.SystemPrompt;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        var userMessage = InputText;
        Messages.Add(new ChatMessage("user", userMessage));

        var messageList = new List<Message>();
        foreach (var msg in Messages)
        {
            messageList.Add(new Message
            {
                Role = msg.Role == "user" ? "user" : "assistant",
                Content = msg.Content
            });
        }

        InputText = string.Empty;
        StatusMessage = "正在回复...";
        IsStreaming = true;

        var assistantMessage = new ChatMessage("assistant", "") { IsStreaming = true };
        Messages.Add(assistantMessage);

        try
        {
            if (UseStreaming)
            {
                await SendStreamingAsync(messageList, assistantMessage);
            }
            else
            {
                await SendNormalAsync(messageList, assistantMessage);
            }
        }
        catch (Exception ex)
        {
            assistantMessage.Content = $"错误: {ex.Message}";
            StatusMessage = "发送失败";
        }
        finally
        {
            assistantMessage.IsStreaming = false;
            IsStreaming = false;
        }
    }

    private async Task SendStreamingAsync(List<Message> messageList, ChatMessage assistantMessage)
    {
        _streamingCts = new CancellationTokenSource();

        try
        {
            await foreach (var chunk in _aiService.SendMessageStreamAsync(
                messageList, SystemPrompt, SelectedModel, _streamingCts.Token))
            {
                if (chunk.IsDone)
                {
                    if (chunk.InputTokens.HasValue)
                    {
                        TotalTokens += chunk.InputTokens.Value + (chunk.OutputTokens ?? 0);
                    }
                    break;
                }

                if (!string.IsNullOrEmpty(chunk.ErrorMessage))
                {
                    assistantMessage.Content = chunk.ErrorMessage;
                    StatusMessage = "回复出错";
                    return;
                }

                assistantMessage.Content += chunk.Content;
            }

            if (_selectedConversation != null)
            {
                _conversationManager.AddMessage(_selectedConversation.Id, new Message
                {
                    Role = "user",
                    Content = Messages[Messages.Count - 2].Content
                });
                _conversationManager.AddMessage(_selectedConversation.Id, new Message
                {
                    Role = "assistant",
                    Content = assistantMessage.Content
                });
            }

            StatusMessage = "对话进行中";
        }
        finally
        {
            _streamingCts?.Dispose();
            _streamingCts = null;
        }
    }

    private async Task SendNormalAsync(List<Message> messageList, ChatMessage assistantMessage)
    {
        var response = await _aiService.SendMessageAsync(messageList, SystemPrompt, SelectedModel);

        if (response.IsSuccess)
        {
            assistantMessage.Content = response.Content;
            assistantMessage.TokenCount = response.OutputTokens;

            if (_selectedConversation != null)
            {
                _conversationManager.AddMessage(_selectedConversation.Id, new Message
                {
                    Role = "user",
                    Content = Messages[Messages.Count - 2].Content
                });
                _conversationManager.AddMessage(_selectedConversation.Id, new Message
                {
                    Role = "assistant",
                    Content = response.Content,
                    TokenCount = response.OutputTokens
                });
            }

            StatusMessage = $"Token: {response.TotalTokens} | 费用: ${response.EstimatedCost:F4}";
        }
        else
        {
            assistantMessage.Content = response.ErrorMessage ?? "未知错误";
            StatusMessage = "回复失败";
        }
    }

    [RelayCommand]
    private void StopStreaming()
    {
        _streamingCts?.Cancel();
        StatusMessage = "已停止生成";
    }

    [RelayCommand]
    private void ClearChat()
    {
        if (_selectedConversation != null)
        {
            _conversationManager.ClearConversation(_selectedConversation.Id);
        }
        Messages.Clear();
        TotalTokens = 0;
        TotalCost = 0;
        StatusMessage = "对话已清空";
    }

    [RelayCommand]
    private void DeleteConversation()
    {
        if (_selectedConversation != null)
        {
            _conversationManager.DeleteConversation(_selectedConversation.Id);
            Conversations.Remove(_selectedConversation);
            CreateNewConversation();
        }
    }

    [RelayCommand]
    private void PinMessage(ChatMessage? message)
    {
        if (message != null)
        {
            message.IsPinned = !message.IsPinned;
            StatusMessage = message.IsPinned ? "消息已置顶" : "消息已取消置顶";
        }
    }

    [RelayCommand]
    private void DeleteMessage(ChatMessage? message)
    {
        if (message != null)
        {
            Messages.Remove(message);
            StatusMessage = "消息已删除";
        }
    }

    private void UpdateContextInfo()
    {
        ContextInfo.Clear();

        var messageList = new List<Message>();
        foreach (var msg in Messages)
        {
            messageList.Add(new Message { Role = msg.Role, Content = msg.Content });
        }

        var info = _contextManager.GetContextInfo(messageList, 4096);

        ContextInfo.Add(new ContextInfoItem
        {
            Label = "消息数",
            Value = info.TotalMessages.ToString()
        });
        ContextInfo.Add(new ContextInfoItem
        {
            Label = "Token使用",
            Value = $"{info.CurrentTokens} / {info.MaxTokens}"
        });
        ContextInfo.Add(new ContextInfoItem
        {
            Label = "使用率",
            Value = $"{info.UsagePercentage:F1}%"
        });
    }
}

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _role;

    [ObservableProperty]
    private string _content;

    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private int _tokenCount;

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.Now;
    }
}

public class ConversationItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class ContextInfoItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

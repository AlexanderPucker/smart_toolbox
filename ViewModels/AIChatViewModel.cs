using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _role;
    
    [ObservableProperty]
    private string _content;
    
    [ObservableProperty]
    private DateTime _timestamp;

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.Now;
    }
}

public partial class AIChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "输入消息开始对话";

    [ObservableProperty]
    private string _systemPrompt = "你是一个友好的AI助手";

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    private readonly AIService _aiService;

    public AIChatViewModel()
    {
        _aiService = new AIService();
        var config = AIConfigManager.LoadConfig();
        _aiService.Configure(config);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        var userMessage = InputText;
        Messages.Add(new ChatMessage("user", userMessage));
        InputText = string.Empty;
        StatusMessage = "正在回复...";

        try
        {
            var messageList = new List<Message>();
            foreach (var msg in Messages)
            {
                messageList.Add(new Message
                {
                    Role = msg.Role == "user" ? "user" : "assistant",
                    Content = msg.Content
                });
            }

            var response = await _aiService.SendMessageAsync(messageList, SystemPrompt);
            Messages.Add(new ChatMessage("assistant", response));
            StatusMessage = "对话进行中";
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage("assistant", $"错误: {ex.Message}"));
            StatusMessage = "发送失败";
        }
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
        StatusMessage = "对话已清空";
    }
}

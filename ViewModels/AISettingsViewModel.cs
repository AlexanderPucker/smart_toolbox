using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class AISettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _selectedProvider = "OpenAI";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _apiUrl = string.Empty;

    [ObservableProperty]
    private string _model = "gpt-3.5-turbo";

    [ObservableProperty]
    private double _temperature = 0.7;

    [ObservableProperty]
    private int _maxTokens = 2000;

    [ObservableProperty]
    private string _statusMessage = "配置AI API参数";

    public ObservableCollection<string> Providers { get; } = new()
    {
        "OpenAI",
        "通义千问",
        "自定义"
    };

    public ObservableCollection<string> OpenAIModels { get; } = new()
    {
        "gpt-4o",
        "gpt-4",
        "gpt-3.5-turbo"
    };

    public ObservableCollection<string> QwenModels { get; } = new()
    {
        "qwen-plus",
        "qwen-turbo",
        "qwen-max"
    };

    public AISettingsViewModel()
    {
        LoadConfig();
    }

    private void LoadConfig()
    {
        var config = AIConfigManager.LoadConfig();
        
        SelectedProvider = config.Provider switch
        {
            AIProvider.OpenAI => "OpenAI",
            AIProvider.Qwen => "通义千问",
            AIProvider.Custom => "自定义",
            _ => "OpenAI"
        };

        ApiKey = config.ApiKey;
        ApiUrl = config.ApiUrl;
        Model = config.Model;
        Temperature = config.Temperature;
        MaxTokens = config.MaxTokens;
    }

    [RelayCommand]
    private void SaveConfig()
    {
        var provider = SelectedProvider switch
        {
            "OpenAI" => AIProvider.OpenAI,
            "通义千问" => AIProvider.Qwen,
            _ => AIProvider.Custom
        };

        var config = new AIConfig
        {
            Provider = provider,
            ApiKey = ApiKey,
            ApiUrl = ApiUrl,
            Model = Model,
            Temperature = Temperature,
            MaxTokens = MaxTokens
        };

        AIConfigManager.SaveConfig(config);
        StatusMessage = "配置已保存";
    }

    [RelayCommand]
    private void ResetConfig()
    {
        ApiKey = string.Empty;
        
        if (SelectedProvider == "OpenAI")
        {
            ApiUrl = "https://api.openai.com/v1/chat/completions";
            Model = "gpt-3.5-turbo";
        }
        else if (SelectedProvider == "通义千问")
        {
            ApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
            Model = "qwen-plus";
        }
        else
        {
            ApiUrl = string.Empty;
            Model = string.Empty;
        }
        
        Temperature = 0.7;
        MaxTokens = 2000;
        StatusMessage = "已重置为默认配置";
    }

    partial void OnSelectedProviderChanged(string value)
    {
        if (value == "OpenAI")
        {
            ApiUrl = "https://api.openai.com/v1/chat/completions";
            Model = "gpt-3.5-turbo";
        }
        else if (value == "通义千问")
        {
            ApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
            Model = "qwen-plus";
        }
    }
}

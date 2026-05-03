using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public static class AIConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartToolbox",
        "ai_config.json");

    private static AIConfig? _cachedConfig;

    public static AIConfig LoadConfig()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        if (!File.Exists(ConfigPath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AIConfig>(json);
            _cachedConfig = config ?? CreateDefaultConfig();
            return _cachedConfig;
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    public static void SaveConfig(AIConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
            _cachedConfig = config;

            AIService.Instance.Configure(config);
        }
        catch { }
    }

    private static AIConfig CreateDefaultConfig()
    {
        _cachedConfig = new AIConfig
        {
            Provider = AIProvider.OpenAI,
            ApiKey = "",
            ApiUrl = "https://api.openai.com/v1/chat/completions",
            Model = "gpt-3.5-turbo",
            Temperature = 0.7,
            MaxTokens = 2000,
            TimeoutSeconds = 120,
            MaxRetries = 3,
            EnableStreaming = true,
            ProviderConfigs = new Dictionary<AIProvider, ProviderConfig>
            {
                [AIProvider.OpenAI] = new ProviderConfig
                {
                    ApiKey = "",
                    ApiUrl = "https://api.openai.com/v1/chat/completions",
                    DefaultModel = "gpt-4o-mini",
                    IsEnabled = true
                },
                [AIProvider.Claude] = new ProviderConfig
                {
                    ApiKey = "",
                    ApiUrl = "https://api.anthropic.com/v1/messages",
                    DefaultModel = "claude-3-sonnet",
                    IsEnabled = false
                },
                [AIProvider.Qwen] = new ProviderConfig
                {
                    ApiKey = "",
                    ApiUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                    DefaultModel = "qwen-turbo",
                    IsEnabled = false
                },
                [AIProvider.DeepSeek] = new ProviderConfig
                {
                    ApiKey = "",
                    ApiUrl = "https://api.deepseek.com/v1/chat/completions",
                    DefaultModel = "deepseek-chat",
                    IsEnabled = false
                },
                [AIProvider.Gemini] = new ProviderConfig
                {
                    ApiKey = "",
                    ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent",
                    DefaultModel = "gemini-pro",
                    IsEnabled = false
                }
            }
        };
        return _cachedConfig;
    }

    public static void UpdateProviderConfig(AIProvider provider, ProviderConfig config)
    {
        var currentConfig = LoadConfig();
        currentConfig.ProviderConfigs[provider] = config;
        SaveConfig(currentConfig);
    }

    public static ProviderConfig? GetProviderConfig(AIProvider provider)
    {
        var config = LoadConfig();
        return config.ProviderConfigs.GetValueOrDefault(provider);
    }

    public static void SetActiveProvider(AIProvider provider)
    {
        var config = LoadConfig();
        config.Provider = provider;

        if (config.ProviderConfigs.TryGetValue(provider, out var providerConfig))
        {
            config.ApiUrl = providerConfig.ApiUrl;
            config.ApiKey = providerConfig.ApiKey;
            config.Model = providerConfig.DefaultModel;
        }

        SaveConfig(config);
    }

    public static void ResetToDefaults()
    {
        _cachedConfig = null;
        var config = CreateDefaultConfig();
        SaveConfig(config);
    }
}

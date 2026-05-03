using System;
using System.Collections.Generic;
using System.Linq;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public enum TaskType
{
    Chat,
    CodeExplanation,
    Translation,
    Summarization,
    RegexGeneration,
    TextPolish,
    Vision,
    LongContext,
    FastResponse
}

public sealed class ModelRouter
{
    private static readonly Lazy<ModelRouter> _instance = new(() => new ModelRouter());
    public static ModelRouter Instance => _instance.Value;

    private readonly Dictionary<string, ModelInfo> _models = new();
    private readonly Dictionary<TaskType, List<string>> _taskModelMapping = new();
    private AIConfig _config;

    public event Action<string>? OnModelSelected;

    private ModelRouter()
    {
        _config = new AIConfig();
        InitializeModels();
        InitializeTaskMapping();
    }

    private void InitializeModels()
    {
        var models = new List<ModelInfo>
        {
            new() { Id = "gpt-4o", DisplayName = "GPT-4o", Provider = AIProvider.OpenAI, ContextWindow = 128000, InputPricePer1K = 0.005, OutputPricePer1K = 0.015, SupportsVision = true, SupportsFunctionCalling = true, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Code, ModelCapability.Vision, ModelCapability.LongContext } },
            new() { Id = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Provider = AIProvider.OpenAI, ContextWindow = 128000, InputPricePer1K = 0.00015, OutputPricePer1K = 0.0006, SupportsFunctionCalling = true, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Fast, ModelCapability.Cheap } },
            new() { Id = "gpt-4-turbo", DisplayName = "GPT-4 Turbo", Provider = AIProvider.OpenAI, ContextWindow = 128000, InputPricePer1K = 0.01, OutputPricePer1K = 0.03, SupportsVision = true, SupportsFunctionCalling = true, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Code, ModelCapability.Vision } },
            new() { Id = "gpt-3.5-turbo", DisplayName = "GPT-3.5 Turbo", Provider = AIProvider.OpenAI, ContextWindow = 16385, InputPricePer1K = 0.0005, OutputPricePer1K = 0.0015, SupportsFunctionCalling = true, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Fast, ModelCapability.Cheap } },
            new() { Id = "claude-3-opus", DisplayName = "Claude 3 Opus", Provider = AIProvider.Claude, ContextWindow = 200000, InputPricePer1K = 0.015, OutputPricePer1K = 0.075, SupportsVision = true, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Code, ModelCapability.LongContext } },
            new() { Id = "claude-3-sonnet", DisplayName = "Claude 3 Sonnet", Provider = AIProvider.Claude, ContextWindow = 200000, InputPricePer1K = 0.003, OutputPricePer1K = 0.015, SupportsVision = true, SupportsFunctionCalling = true, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Code, ModelCapability.Vision } },
            new() { Id = "claude-3-haiku", DisplayName = "Claude 3 Haiku", Provider = AIProvider.Claude, ContextWindow = 200000, InputPricePer1K = 0.00025, OutputPricePer1K = 0.00125, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Fast, ModelCapability.Cheap } },
            new() { Id = "qwen-turbo", DisplayName = "Qwen Turbo", Provider = AIProvider.Qwen, ContextWindow = 8192, InputPricePer1K = 0.0002, OutputPricePer1K = 0.0006, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Fast, ModelCapability.Cheap } },
            new() { Id = "qwen-plus", DisplayName = "Qwen Plus", Provider = AIProvider.Qwen, ContextWindow = 32768, InputPricePer1K = 0.0004, OutputPricePer1K = 0.0012, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Code } },
            new() { Id = "qwen-max", DisplayName = "Qwen Max", Provider = AIProvider.Qwen, ContextWindow = 32768, InputPricePer1K = 0.002, OutputPricePer1K = 0.006, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Code, ModelCapability.LongContext } },
            new() { Id = "deepseek-chat", DisplayName = "DeepSeek Chat", Provider = AIProvider.DeepSeek, ContextWindow = 64000, InputPricePer1K = 0.0001, OutputPricePer1K = 0.0002, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Cheap } },
            new() { Id = "deepseek-coder", DisplayName = "DeepSeek Coder", Provider = AIProvider.DeepSeek, ContextWindow = 64000, InputPricePer1K = 0.0001, OutputPricePer1K = 0.0002, Capabilities = new List<ModelCapability> { ModelCapability.Code, ModelCapability.Cheap } },
            new() { Id = "gemini-pro", DisplayName = "Gemini Pro", Provider = AIProvider.Gemini, ContextWindow = 32760, InputPricePer1K = 0.00025, OutputPricePer1K = 0.0005, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Cheap } },
            new() { Id = "gemini-1.5-pro", DisplayName = "Gemini 1.5 Pro", Provider = AIProvider.Gemini, ContextWindow = 1000000, InputPricePer1K = 0.00125, OutputPricePer1K = 0.005, SupportsVision = true, Capabilities = new List<ModelCapability> { ModelCapability.Chat, ModelCapability.Code, ModelCapability.Vision, ModelCapability.LongContext } },
        };

        foreach (var model in models)
        {
            _models[model.Id] = model;
        }
    }

    private void InitializeTaskMapping()
    {
        _taskModelMapping[TaskType.Chat] = new List<string> { "gpt-4o-mini", "gpt-3.5-turbo", "claude-3-haiku", "qwen-turbo", "deepseek-chat" };
        _taskModelMapping[TaskType.CodeExplanation] = new List<string> { "gpt-4o", "claude-3-sonnet", "deepseek-coder", "gpt-4-turbo" };
        _taskModelMapping[TaskType.Translation] = new List<string> { "gpt-4o-mini", "claude-3-haiku", "qwen-turbo", "gpt-3.5-turbo" };
        _taskModelMapping[TaskType.Summarization] = new List<string> { "gpt-4o", "claude-3-sonnet", "gemini-1.5-pro", "qwen-max" };
        _taskModelMapping[TaskType.RegexGeneration] = new List<string> { "gpt-4o-mini", "gpt-3.5-turbo", "claude-3-haiku" };
        _taskModelMapping[TaskType.TextPolish] = new List<string> { "gpt-4o", "claude-3-sonnet", "qwen-plus" };
        _taskModelMapping[TaskType.Vision] = new List<string> { "gpt-4o", "claude-3-sonnet", "gemini-1.5-pro", "gpt-4-turbo" };
        _taskModelMapping[TaskType.LongContext] = new List<string> { "gemini-1.5-pro", "claude-3-opus", "gpt-4o" };
        _taskModelMapping[TaskType.FastResponse] = new List<string> { "gpt-4o-mini", "claude-3-haiku", "qwen-turbo", "gpt-3.5-turbo" };
    }

    public void Configure(AIConfig config)
    {
        _config = config;
    }

    public string SelectModel(TaskType taskType, int estimatedTokens = 0)
    {
        if (!_taskModelMapping.TryGetValue(taskType, out var candidateModels))
        {
            return _config.Model;
        }

        foreach (var modelId in candidateModels)
        {
            if (_models.TryGetValue(modelId, out var model))
            {
                if (IsModelAvailable(model) && model.ContextWindow >= estimatedTokens)
                {
                    OnModelSelected?.Invoke(modelId);
                    return modelId;
                }
            }
        }

        return _config.Model;
    }

    public string SelectModelForCode()
    {
        return SelectModel(TaskType.CodeExplanation);
    }

    public string SelectModelForTranslation()
    {
        return SelectModel(TaskType.Translation);
    }

    public string SelectModelForSummarization(int estimatedTokens = 0)
    {
        return SelectModel(TaskType.Summarization, estimatedTokens);
    }

    public string SelectModelForVision()
    {
        return SelectModel(TaskType.Vision);
    }

    public string SelectFastModel()
    {
        return SelectModel(TaskType.FastResponse);
    }

    public string SelectCheapModel()
    {
        var cheapModels = _models.Values
            .Where(m => m.Capabilities.Contains(ModelCapability.Cheap) && IsModelAvailable(m))
            .OrderBy(m => m.InputPricePer1K)
            .ToList();

        return cheapModels.FirstOrDefault()?.Id ?? _config.Model;
    }

    private bool IsModelAvailable(ModelInfo model)
    {
        return _config.ProviderConfigs.TryGetValue(model.Provider, out var providerConfig) 
            ? providerConfig.IsEnabled && !string.IsNullOrEmpty(providerConfig.ApiKey)
            : model.Provider == _config.Provider && !string.IsNullOrEmpty(_config.ApiKey);
    }

    public ModelInfo? GetModelInfo(string modelId)
    {
        return _models.GetValueOrDefault(modelId);
    }

    public List<ModelInfo> GetAllModels()
    {
        return _models.Values.ToList();
    }

    public List<ModelInfo> GetAvailableModels()
    {
        return _models.Values.Where(IsModelAvailable).ToList();
    }

    public List<ModelInfo> GetModelsByCapability(ModelCapability capability)
    {
        return _models.Values.Where(m => m.Capabilities.Contains(capability)).ToList();
    }

    public void AddCustomModel(ModelInfo model)
    {
        _models[model.Id] = model;
    }

    public void SetTaskModelMapping(TaskType taskType, List<string> modelIds)
    {
        _taskModelMapping[taskType] = modelIds;
    }

    public string GetProviderForModel(string modelId)
    {
        if (_models.TryGetValue(modelId, out var model))
        {
            return model.Provider.ToString();
        }
        return _config.Provider.ToString();
    }
}

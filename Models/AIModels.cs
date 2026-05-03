using System;
using System.Collections.Generic;

namespace SmartToolbox.Models;

public enum AIProvider
{
    OpenAI,
    Qwen,
    Claude,
    DeepSeek,
    Gemini,
    Custom
}

public enum ModelCapability
{
    Chat,
    Code,
    Translation,
    Summarization,
    Vision,
    LongContext,
    Fast,
    Cheap
}

public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AIProvider Provider { get; set; }
    public int ContextWindow { get; set; } = 4096;
    public double InputPricePer1K { get; set; }
    public double OutputPricePer1K { get; set; }
    public List<ModelCapability> Capabilities { get; set; } = new();
    public bool SupportsStreaming { get; set; } = true;
    public bool SupportsVision { get; set; } = false;
    public bool SupportsFunctionCalling { get; set; } = false;
}

public class AIConfig
{
    public AIProvider Provider { get; set; } = AIProvider.OpenAI;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 3;
    public bool EnableStreaming { get; set; } = true;
    public Dictionary<AIProvider, ProviderConfig> ProviderConfigs { get; set; } = new();
}

public class ProviderConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class Message
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public List<ImageContent>? Images { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public int TokenCount { get; set; }
    public bool IsPinned { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
}

public class ImageContent
{
    public string Type { get; set; } = "url";
    public string Data { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image/png";
}

public class ChatCompletionRequest
{
    public string Model { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Messages { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public bool Stream { get; set; }
    public List<ToolDefinition>? Tools { get; set; }
}

public class ToolDefinition
{
    public string Type { get; set; } = "function";
    public FunctionDefinition Function { get; set; } = new();
}

public class FunctionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object Parameters { get; set; } = new { };
}

public class ToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "function";
    public FunctionCall Function { get; set; } = new();
}

public class FunctionCall
{
    public string Name { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}

public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public string Model { get; set; } = string.Empty;
    public AIProvider Provider { get; set; }
    public double EstimatedCost { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public bool IsToolCall => ToolCalls != null && ToolCalls.Count > 0;
    public TimeSpan Duration { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class StreamingChunk
{
    public string Content { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
    public string? ErrorMessage { get; set; }
}

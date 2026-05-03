using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public class LocalModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ModifiedAt { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Digest { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class OllamaResponse
{
    public string Model { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool Done { get; set; }
    public long? PromptEvalCount { get; set; }
    public long? EvalCount { get; set; }
    public long? TotalDuration { get; set; }
}

public class OllamaModelPullProgress
{
    public string Status { get; set; } = string.Empty;
    public string? Digest { get; set; }
    public long? Total { get; set; }
    public long? Completed { get; set; }
}

public sealed class OllamaService
{
    private static readonly Lazy<OllamaService> _instance = new(() => new OllamaService());
    public static OllamaService Instance => _instance.Value;

    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://localhost:11434";
    private bool _isEnabled;
    private List<LocalModelInfo> _cachedModels = new();

    public event Action<List<LocalModelInfo>>? OnModelsUpdated;
    public event Action<string, OllamaModelPullProgress>? OnPullProgress;
    public event Action<bool>? OnConnectionStatusChanged;

    public bool IsEnabled => _isEnabled;
    public string BaseUrl => _baseUrl;

    private OllamaService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
    }

    public void Configure(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", 
                new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            var wasEnabled = _isEnabled;
            _isEnabled = response.IsSuccessStatusCode;
            
            if (wasEnabled != _isEnabled)
            {
                OnConnectionStatusChanged?.Invoke(_isEnabled);
            }
            
            return _isEnabled;
        }
        catch
        {
            _isEnabled = false;
            OnConnectionStatusChanged?.Invoke(false);
            return false;
        }
    }

    public async Task<List<LocalModelInfo>> ListModelsAsync()
    {
        if (!_isEnabled)
        {
            await CheckConnectionAsync();
        }

        if (!_isEnabled) return new List<LocalModelInfo>();

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaModelsResponse>(content);

            _cachedModels = result?.Models?.Select(m => new LocalModelInfo
            {
                Name = m.Name ?? string.Empty,
                Model = m.Model ?? m.Name ?? string.Empty,
                ModifiedAt = m.ModifiedAt ?? string.Empty,
                Size = m.Size,
                Digest = m.Digest ?? string.Empty,
                Details = m.Details?.ToString() ?? string.Empty
            }).ToList() ?? new List<LocalModelInfo>();

            OnModelsUpdated?.Invoke(_cachedModels);
            return _cachedModels;
        }
        catch
        {
            return new List<LocalModelInfo>();
        }
    }

    public async Task<AIResponse> SendMessageAsync(
        string model,
        string prompt,
        string systemPrompt = "",
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var request = new
            {
                model,
                prompt,
                system = systemPrompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new AIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ollama 请求失败: {response.StatusCode}",
                    Model = model,
                    Provider = AIProvider.Custom
                };
            }

            var result = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

            if (result == null)
            {
                return new AIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "无法解析 Ollama 响应",
                    Model = model
                };
            }

            var duration = DateTime.Now - startTime;

            return new AIResponse
            {
                Content = result.Response,
                InputTokens = (int)(result.PromptEvalCount ?? 0),
                OutputTokens = (int)(result.EvalCount ?? 0),
                Model = model,
                Provider = AIProvider.Custom,
                EstimatedCost = 0,
                Duration = duration,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Ollama 错误: {ex.Message}",
                Model = model
            };
        }
    }

    public async IAsyncEnumerable<StreamingChunk> SendMessageStreamAsync(
        string model,
        string prompt,
        string systemPrompt = "",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        var request = new
        {
            model,
            prompt,
            system = systemPrompt,
            stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cancellationToken);
        }
        catch (Exception ex)
        {
            yield return new StreamingChunk { ErrorMessage = $"连接失败: {ex.Message}" };
            yield break;
        }

        if (!response.IsSuccessStatusCode)
        {
            yield return new StreamingChunk { ErrorMessage = $"请求失败: {response.StatusCode}" };
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        int inputTokens = 0, outputTokens = 0;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var chunk = JsonSerializer.Deserialize<OllamaResponse>(line);
                if (chunk == null) continue;

                if (!string.IsNullOrEmpty(chunk.Response))
                {
                    yield return new StreamingChunk
                    {
                        Content = chunk.Response,
                        IsDone = false
                    };
                }

                if (chunk.Done)
                {
                    inputTokens = (int)(chunk.PromptEvalCount ?? 0);
                    outputTokens = (int)(chunk.EvalCount ?? 0);

                    yield return new StreamingChunk
                    {
                        IsDone = true,
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens
                    };
                    yield break;
                }
            }
            catch { }
        }
    }

    public async Task PullModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        var request = new { name = modelName, stream = true };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/pull", content, cancellationToken);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var progress = JsonSerializer.Deserialize<OllamaModelPullProgress>(line);
                if (progress != null)
                {
                    OnPullProgress?.Invoke(modelName, progress);
                }
            }
            catch { }
        }

        await ListModelsAsync();
    }

    public async Task DeleteModelAsync(string modelName)
    {
        var request = new { name = modelName };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await _httpClient.DeleteAsync($"{_baseUrl}/api/delete?name={Uri.EscapeDataString(modelName)}");
        await ListModelsAsync();
    }

    public async Task<bool> IsModelAvailableAsync(string modelName)
    {
        var models = await ListModelsAsync();
        return models.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
                               m.Name.StartsWith(modelName, StringComparison.OrdinalIgnoreCase));
    }

    public List<string> GetRecommendedModels()
    {
        return new List<string>
        {
            "llama3.2:latest",
            "llama3.1:latest",
            "mistral:latest",
            "codellama:latest",
            "deepseek-coder:latest",
            "qwen2.5:latest",
            "gemma2:latest",
            "phi3:latest",
            "llava:latest"
        };
    }

    public string GetModelDescription(string modelName)
    {
        return modelName.ToLower() switch
        {
            var m when m.Contains("llama3.2") => "Meta Llama 3.2 - 轻量级，适合日常对话",
            var m when m.Contains("llama3.1") => "Meta Llama 3.1 - 强大的通用模型",
            var m when m.Contains("mistral") => "Mistral - 高效的开源模型",
            var m when m.Contains("codellama") => "Code Llama - 专门用于代码生成",
            var m when m.Contains("deepseek") => "DeepSeek Coder - 代码专家",
            var m when m.Contains("qwen") => "通义千问 - 中文优化",
            var m when m.Contains("gemma") => "Google Gemma - 轻量高效",
            var m when m.Contains("phi") => "Microsoft Phi - 超轻量模型",
            var m when m.Contains("llava") => "LLaVA - 支持视觉的多模态模型",
            _ => "本地大语言模型"
        };
    }

    public async Task<string> GetModelInfoAsync(string modelName)
    {
        try
        {
            var request = new { name = modelName };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/show", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }
        catch
        {
            return "无法获取模型信息";
        }
    }
}

internal class OllamaModelsResponse
{
    public List<OllamaModelItem>? Models { get; set; }
}

internal class OllamaModelItem
{
    public string? Name { get; set; }
    public string? Model { get; set; }
    public string? ModifiedAt { get; set; }
    public long Size { get; set; }
    public string? Digest { get; set; }
    public object? Details { get; set; }
}

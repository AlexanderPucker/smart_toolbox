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

public sealed class AIService
{
    private static readonly Lazy<AIService> _instance = new(() => new AIService());
    public static AIService Instance => _instance.Value;

    private readonly HttpClient _httpClient;
    private AIConfig _config;
    private readonly TokenCounterService _tokenCounter;
    private readonly RateLimiter _rateLimiter;
    private readonly ModelRouter _modelRouter;
    private readonly ContextWindowManager _contextManager;

    public event Action<string>? OnStreamingChunk;
    public event Action<AIResponse>? OnResponseReceived;
    public event Action<string>? OnError;

    private AIService()
    {
        _httpClient = new HttpClient();
        _config = new AIConfig();
        _tokenCounter = TokenCounterService.Instance;
        _rateLimiter = RateLimiter.Instance;
        _modelRouter = ModelRouter.Instance;
        _contextManager = ContextWindowManager.Instance;
    }

    public void Configure(AIConfig config)
    {
        _config = config;
        _modelRouter.Configure(config);
        _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_config.ApiKey) && !string.IsNullOrEmpty(_config.ApiUrl);
    }

    public async Task<AIResponse> SendMessageAsync(
        string prompt,
        string systemPrompt = "",
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<Message>
        {
            new() { Role = "user", Content = prompt }
        };
        return await SendMessageAsync(messages, systemPrompt, model, cancellationToken);
    }

    public async Task<AIResponse> SendMessageAsync(
        List<Message> messages,
        string systemPrompt = "",
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var useModel = model ?? _config.Model;
        var startTime = DateTime.Now;

        if (!IsConfigured())
        {
            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = "请先在设置中配置API密钥"
            };
        }

        var preparedMessages = _contextManager.PrepareMessagesForRequest(messages, systemPrompt, useModel);

        return await ExecuteWithRetryAsync(async () =>
        {
            var request = BuildRequest(preparedMessages, systemPrompt, useModel);
            return await SendRequestAsync(request, useModel, startTime, cancellationToken);
        }, cancellationToken);
    }

    public async IAsyncEnumerable<StreamingChunk> SendMessageStreamAsync(
        List<Message> messages,
        string systemPrompt = "",
        string? model = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var useModel = model ?? _config.Model;
        var startTime = DateTime.Now;

        if (!IsConfigured())
        {
            yield return new StreamingChunk { ErrorMessage = "请先在设置中配置API密钥" };
            yield break;
        }

        var preparedMessages = _contextManager.PrepareMessagesForRequest(messages, systemPrompt, useModel);
        var request = BuildRequest(preparedMessages, systemPrompt, useModel, stream: true);

        var response = await SendStreamRequestAsync(request, cancellationToken);

        await foreach (var chunk in ProcessStreamResponseAsync(response, useModel, startTime, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<AIResponse> SendMessageWithToolsAsync(
        List<Message> messages,
        List<ToolDefinition> tools,
        string systemPrompt = "",
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var useModel = model ?? _config.Model;
        var startTime = DateTime.Now;

        if (!IsConfigured())
        {
            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = "请先在设置中配置API密钥"
            };
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            var request = BuildRequest(messages, systemPrompt, useModel, tools: tools);
            return await SendRequestAsync(request, useModel, startTime, cancellationToken);
        }, cancellationToken);
    }

    public async Task<AIResponse> SendMessageWithVisionAsync(
        string prompt,
        List<ImageContent> images,
        string systemPrompt = "",
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var useModel = model ?? _modelRouter.SelectModelForVision();
        var startTime = DateTime.Now;

        var message = new Message
        {
            Role = "user",
            Content = prompt,
            Images = images
        };

        var messages = new List<Message> { message };

        return await ExecuteWithRetryAsync(async () =>
        {
            var request = BuildVisionRequest(messages, systemPrompt, useModel);
            return await SendRequestAsync(request, useModel, startTime, cancellationToken);
        }, cancellationToken);
    }

    private ChatCompletionRequest BuildRequest(
        List<Message> messages,
        string systemPrompt,
        string model,
        bool stream = false,
        List<ToolDefinition>? tools = null)
    {
        var apiMessages = new List<Dictionary<string, object>>();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            apiMessages.Add(new Dictionary<string, object>
            {
                { "role", "system" },
                { "content", systemPrompt }
            });
        }

        foreach (var msg in messages)
        {
            if (msg.Images != null && msg.Images.Count > 0)
            {
                var content = new List<object>
                {
                    new { type = "text", text = msg.Content }
                };

                foreach (var img in msg.Images)
                {
                    if (img.Type == "url")
                    {
                        content.Add(new { type = "image_url", image_url = new { url = img.Data } });
                    }
                    else
                    {
                        content.Add(new { type = "image_url", image_url = new { url = $"data:{img.MediaType};base64,{img.Data}" } });
                    }
                }

                apiMessages.Add(new Dictionary<string, object>
                {
                    { "role", msg.Role },
                    { "content", content }
                });
            }
            else
            {
                apiMessages.Add(new Dictionary<string, object>
                {
                    { "role", msg.Role },
                    { "content", msg.Content }
                });
            }
        }

        return new ChatCompletionRequest
        {
            Model = model,
            Messages = apiMessages,
            Temperature = _config.Temperature,
            MaxTokens = _config.MaxTokens,
            Stream = stream,
            Tools = tools
        };
    }

    private ChatCompletionRequest BuildVisionRequest(
        List<Message> messages,
        string systemPrompt,
        string model)
    {
        return BuildRequest(messages, systemPrompt, model);
    }

    private async Task<AIResponse> SendRequestAsync(
        ChatCompletionRequest request,
        string model,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
        var provider = _modelRouter.GetProviderForModel(model);
        var apiUrl = GetApiUrlForProvider(provider);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var apiKey = GetApiKeyForProvider(provider);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        try
        {
            var response = await _httpClient.PostAsync(apiUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new AIResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"请求失败 ({response.StatusCode}): {responseContent}",
                    Model = model,
                    Provider = Enum.Parse<AIProvider>(provider)
                };
            }

            return ParseResponse(responseContent, model, startTime);
        }
        catch (TaskCanceledException)
        {
            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = "请求超时",
                Model = model
            };
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex.Message);
            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = $"请求异常: {ex.Message}",
                Model = model
            };
        }
    }

    private async Task<HttpResponseMessage> SendStreamRequestAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var provider = _modelRouter.GetProviderForModel(request.Model);
        var apiUrl = GetApiUrlForProvider(provider);
        var apiKey = GetApiKeyForProvider(provider);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        return await _httpClient.PostAsync(apiUrl, content, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private async IAsyncEnumerable<StreamingChunk> ProcessStreamResponseAsync(
        HttpResponseMessage response,
        string model,
        DateTime startTime,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return new StreamingChunk { ErrorMessage = $"请求失败 ({response.StatusCode}): {errorContent}" };
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var totalContent = new StringBuilder();
        int? inputTokens = null;
        int? outputTokens = null;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrEmpty(line))
                continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);

                if (data == "[DONE]")
                {
                    var duration = DateTime.Now - startTime;
                    RecordUsage(model, inputTokens ?? 0, outputTokens ?? totalContent.Length / 4, duration);

                    yield return new StreamingChunk
                    {
                        IsDone = true,
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens ?? totalContent.Length / 4
                    };
                    yield break;
                }

                try
                {
                    var chunk = JsonSerializer.Deserialize<JsonElement>(data);
                    var choices = chunk.GetProperty("choices");

                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");

                        if (delta.TryGetProperty("content", out var contentProp))
                        {
                            var chunkContent = contentProp.GetString() ?? "";
                            totalContent.Append(chunkContent);

                            OnStreamingChunk?.Invoke(chunkContent);

                            yield return new StreamingChunk
                            {
                                Content = chunkContent,
                                IsDone = false
                            };
                        }
                    }

                    if (chunk.TryGetProperty("usage", out var usage))
                    {
                        inputTokens = usage.GetProperty("prompt_tokens").GetInt32();
                        outputTokens = usage.GetProperty("completion_tokens").GetInt32();
                    }
                }
                catch { }
            }
        }
    }

    private AIResponse ParseResponse(string responseContent, string model, DateTime startTime)
    {
        try
        {
            var resultDoc = JsonDocument.Parse(responseContent);
            var choices = resultDoc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var messageObj = firstChoice.GetProperty("message");

            var content = messageObj.GetProperty("content").GetString() ?? "";

            int inputTokens = 0, outputTokens = 0;
            if (resultDoc.RootElement.TryGetProperty("usage", out var usage))
            {
                inputTokens = usage.GetProperty("prompt_tokens").GetInt32();
                outputTokens = usage.GetProperty("completion_tokens").GetInt32();
            }
            else
            {
                inputTokens = _tokenCounter.EstimateTokens(content) / 2;
                outputTokens = _tokenCounter.EstimateTokens(content);
            }

            var duration = DateTime.Now - startTime;
            var cost = _tokenCounter.CalculateCost(model, inputTokens, outputTokens);

            var provider = _modelRouter.GetProviderForModel(model);
            var response = new AIResponse
            {
                Content = content,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                Model = model,
                Provider = Enum.Parse<AIProvider>(provider),
                EstimatedCost = cost,
                Duration = duration,
                IsSuccess = true
            };

            if (messageObj.TryGetProperty("tool_calls", out var toolCalls))
            {
                response.ToolCalls = new List<ToolCall>();
                foreach (var tc in toolCalls.EnumerateArray())
                {
                    response.ToolCalls.Add(new ToolCall
                    {
                        Id = tc.GetProperty("id").GetString() ?? "",
                        Type = tc.GetProperty("type").GetString() ?? "function",
                        Function = new FunctionCall
                        {
                            Name = tc.GetProperty("function").GetProperty("name").GetString() ?? "",
                            Arguments = tc.GetProperty("function").GetProperty("arguments").GetString() ?? ""
                        }
                    });
                }
            }

            RecordUsage(model, inputTokens, outputTokens, duration);
            OnResponseReceived?.Invoke(response);

            return response;
        }
        catch (Exception ex)
        {
            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = $"解析响应失败: {ex.Message}",
                Model = model
            };
        }
    }

    private async Task<AIResponse> ExecuteWithRetryAsync(
        Func<Task<AIResponse>> action,
        CancellationToken cancellationToken)
    {
        int maxRetries = _config.MaxRetries;
        int delay = 1000;

        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                var result = await _rateLimiter.ExecuteAsync("default", action, cancellationToken);

                if (result.IsSuccess || i == maxRetries)
                {
                    return result;
                }

                if (!result.IsSuccess && result.ErrorMessage?.Contains("429") == true)
                {
                    delay = Math.Min(delay * 2, 30000);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (Exception ex) when (i < maxRetries)
            {
                OnError?.Invoke($"请求失败，正在重试 ({i + 1}/{maxRetries}): {ex.Message}");
                await Task.Delay(delay, cancellationToken);
                delay = Math.Min(delay * 2, 30000);
            }
        }

        return new AIResponse
        {
            IsSuccess = false,
            ErrorMessage = "超过最大重试次数"
        };
    }

    private string GetApiUrlForProvider(string provider)
    {
        if (_config.ProviderConfigs.TryGetValue(Enum.Parse<AIProvider>(provider), out var providerConfig))
        {
            return providerConfig.ApiUrl;
        }

        return provider switch
        {
            "OpenAI" => "https://api.openai.com/v1/chat/completions",
            "Claude" => "https://api.anthropic.com/v1/messages",
            "Qwen" => "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
            "DeepSeek" => "https://api.deepseek.com/v1/chat/completions",
            "Gemini" => "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent",
            _ => _config.ApiUrl
        };
    }

    private string GetApiKeyForProvider(string provider)
    {
        if (_config.ProviderConfigs.TryGetValue(Enum.Parse<AIProvider>(provider), out var providerConfig) 
            && !string.IsNullOrEmpty(providerConfig.ApiKey))
        {
            return providerConfig.ApiKey;
        }

        return _config.ApiKey;
    }

    private void RecordUsage(string model, int inputTokens, int outputTokens, TimeSpan duration)
    {
        var record = new UsageRecord
        {
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            Cost = _tokenCounter.CalculateCost(model, inputTokens, outputTokens),
            Duration = duration,
            IsStreaming = true,
            Timestamp = DateTime.Now
        };

        _tokenCounter.RecordUsage(record);
    }

    public async Task<bool> TestConnectionAsync(string? model = null)
    {
        try
        {
            var response = await SendMessageAsync("Hello", "You are a helpful assistant.", model);
            return response.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    public List<ModelInfo> GetAvailableModels()
    {
        return _modelRouter.GetAvailableModels();
    }
}

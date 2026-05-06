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
            var preparedMessages = _contextManager.PrepareMessagesForRequest(messages, systemPrompt, useModel);
            var request = BuildRequest(preparedMessages, systemPrompt, useModel, tools: tools);
            return await SendRequestAsync(request, useModel, startTime, cancellationToken);
        }, cancellationToken);
    }

    public async Task<AIResponse> SendMessageWithToolLoopAsync(
        List<Message> messages,
        List<ToolDefinition> tools,
        Func<string, string, Task<string>> toolExecutor,
        string systemPrompt = "",
        string? model = null,
        int maxRounds = 6,
        CancellationToken cancellationToken = default)
    {
        var useModel = model ?? _config.Model;
        if (!IsConfigured())
        {
            return new AIResponse
            {
                IsSuccess = false,
                ErrorMessage = "请先在设置中配置API密钥"
            };
        }

        if (!SupportsFunctionCalling(useModel))
        {
            return await SendMessageAsync(messages, systemPrompt, useModel, cancellationToken);
        }

        var conversation = messages.Select(CloneMessage).ToList();
        var aggregateResponse = new AIResponse
        {
            Model = useModel,
            Provider = Enum.Parse<AIProvider>(_modelRouter.GetProviderForModel(useModel)),
            ResponseMessages = new List<Message>()
        };

        // 这里实现标准的 tool-calling 闭环：模型请求工具 -> 本地执行 -> 把结果回填给模型。
        for (int round = 0; round < maxRounds; round++)
        {
            var startTime = DateTime.Now;
            var preparedMessages = _contextManager.PrepareMessagesForRequest(conversation, systemPrompt, useModel);
            var response = await ExecuteWithRetryAsync(async () =>
            {
                var request = BuildRequest(preparedMessages, systemPrompt, useModel, tools: tools);
                return await SendRequestAsync(request, useModel, startTime, cancellationToken, publishEvent: false);
            }, cancellationToken);

            if (!response.IsSuccess)
            {
                return response;
            }

            aggregateResponse.InputTokens += response.InputTokens;
            aggregateResponse.OutputTokens += response.OutputTokens;
            aggregateResponse.EstimatedCost += response.EstimatedCost;
            aggregateResponse.Duration += response.Duration;
            aggregateResponse.Content = response.Content;

            if (response.ResponseMessages != null)
            {
                foreach (var message in response.ResponseMessages)
                {
                    aggregateResponse.ResponseMessages!.Add(CloneMessage(message));
                    conversation.Add(CloneMessage(message));
                }
            }

            if (!response.IsToolCall || response.ToolCalls == null || response.ToolCalls.Count == 0)
            {
                aggregateResponse.IsSuccess = true;
                OnResponseReceived?.Invoke(aggregateResponse);
                return aggregateResponse;
            }

            foreach (var toolCall in response.ToolCalls)
            {
                var toolResult = await toolExecutor(toolCall.Function.Name, toolCall.Function.Arguments);
                var toolMessage = new Message
                {
                    Role = "tool",
                    Content = toolResult,
                    ToolCallId = toolCall.Id,
                    ToolName = toolCall.Function.Name
                };
                conversation.Add(toolMessage);
                aggregateResponse.ResponseMessages!.Add(CloneMessage(toolMessage));
            }
        }

        return new AIResponse
        {
            IsSuccess = false,
            ErrorMessage = "工具调用轮次超过上限",
            Model = useModel
        };
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

    public async Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        List<string> inputs,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return new EmbeddingResponse
            {
                IsSuccess = false,
                ErrorMessage = "请先在设置中配置API密钥"
            };
        }

        if (inputs.Count == 0)
        {
            return new EmbeddingResponse
            {
                IsSuccess = true,
                Embeddings = new List<EmbeddingResult>()
            };
        }

        var embeddingModel = model ?? GetDefaultEmbeddingModel();
        if (string.IsNullOrEmpty(embeddingModel))
        {
            return new EmbeddingResponse
            {
                IsSuccess = false,
                ErrorMessage = "当前 Provider 未配置可用的 Embedding 模型"
            };
        }

        var provider = _modelRouter.GetProviderForModel(_config.Model);
        var endpoint = GetEmbeddingApiUrl(provider);
        if (string.IsNullOrEmpty(endpoint))
        {
            return new EmbeddingResponse
            {
                IsSuccess = false,
                ErrorMessage = "当前 Provider 暂不支持 Embedding 接口"
            };
        }

        var request = new EmbeddingRequest
        {
            Model = embeddingModel,
            Input = inputs
        };

        try
        {
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {GetApiKeyForProvider(provider)}");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new EmbeddingResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"Embedding 请求失败 ({response.StatusCode}): {responseContent}",
                    Model = embeddingModel
                };
            }

            var doc = JsonDocument.Parse(responseContent);
            var results = new List<EmbeddingResult>();
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var item in data.EnumerateArray())
                {
                    var vector = item.GetProperty("embedding")
                        .EnumerateArray()
                        .Select(v => v.GetSingle())
                        .ToArray();
                    results.Add(new EmbeddingResult
                    {
                        Index = item.TryGetProperty("index", out var index) ? index.GetInt32() : results.Count,
                        Vector = vector
                    });
                }
            }

            return new EmbeddingResponse
            {
                IsSuccess = true,
                Model = embeddingModel,
                Embeddings = results.OrderBy(r => r.Index).ToList()
            };
        }
        catch (Exception ex)
        {
            return new EmbeddingResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Embedding 请求异常: {ex.Message}",
                Model = embeddingModel
            };
        }
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
            // assistant/tool 两种消息需要保留函数调用上下文，后续轮次模型才能继续推理。
            else if (msg.Role == "assistant" && msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                var assistantMessage = new Dictionary<string, object>
                {
                    { "role", msg.Role },
                    { "tool_calls", msg.ToolCalls.Select(tc => new Dictionary<string, object>
                        {
                            { "id", tc.Id },
                            { "type", tc.Type },
                            { "function", new Dictionary<string, object>
                                {
                                    { "name", tc.Function.Name },
                                    { "arguments", tc.Function.Arguments }
                                }
                            }
                        }).ToList() }
                };

                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    assistantMessage["content"] = msg.Content;
                }

                apiMessages.Add(assistantMessage);
            }
            else if (msg.Role == "tool")
            {
                var toolMessage = new Dictionary<string, object>
                {
                    { "role", msg.Role },
                    { "content", msg.Content },
                    { "tool_call_id", msg.ToolCallId ?? string.Empty }
                };

                if (!string.IsNullOrWhiteSpace(msg.ToolName))
                {
                    toolMessage["name"] = msg.ToolName;
                }

                apiMessages.Add(toolMessage);
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
        CancellationToken cancellationToken,
        bool publishEvent = true)
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

            return ParseResponse(responseContent, model, startTime, publishEvent);
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

    private AIResponse ParseResponse(string responseContent, string model, DateTime startTime, bool publishEvent = true)
    {
        try
        {
            var resultDoc = JsonDocument.Parse(responseContent);
            var choices = resultDoc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var messageObj = firstChoice.GetProperty("message");

            var content = messageObj.TryGetProperty("content", out var contentProp) ? contentProp.GetString() ?? "" : "";

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
                IsSuccess = true,
                ResponseMessages = new List<Message>()
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

            response.ResponseMessages.Add(new Message
            {
                Role = "assistant",
                Content = content,
                ToolCalls = response.ToolCalls?.Select(CloneToolCall).ToList(),
                Timestamp = DateTime.Now,
                TokenCount = outputTokens
            });

            RecordUsage(model, inputTokens, outputTokens, duration);
            if (publishEvent)
            {
                OnResponseReceived?.Invoke(response);
            }

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

    private string GetEmbeddingApiUrl(string provider)
    {
        var chatUrl = GetApiUrlForProvider(provider);
        return provider switch
        {
            "OpenAI" or "DeepSeek" => chatUrl.Replace("/chat/completions", "/embeddings", StringComparison.OrdinalIgnoreCase),
            _ => string.Empty
        };
    }

    private string GetDefaultEmbeddingModel()
    {
        var provider = _modelRouter.GetProviderForModel(_config.Model);
        return provider switch
        {
            "OpenAI" => "text-embedding-3-small",
            "DeepSeek" => "text-embedding-3-small",
            _ => string.Empty
        };
    }

    private bool SupportsFunctionCalling(string model)
    {
        return _modelRouter.GetModelInfo(model)?.SupportsFunctionCalling == true;
    }

    private static Message CloneMessage(Message source)
    {
        return new Message
        {
            Role = source.Role,
            Content = source.Content,
            Images = source.Images,
            ToolCalls = source.ToolCalls?.Select(CloneToolCall).ToList(),
            Timestamp = source.Timestamp,
            TokenCount = source.TokenCount,
            IsPinned = source.IsPinned,
            ToolCallId = source.ToolCallId,
            ToolName = source.ToolName
        };
    }

    private static ToolCall CloneToolCall(ToolCall source)
    {
        return new ToolCall
        {
            Id = source.Id,
            Type = source.Type,
            Function = new FunctionCall
            {
                Name = source.Function.Name,
                Arguments = source.Function.Arguments
            }
        };
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

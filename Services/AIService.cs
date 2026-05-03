using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public enum AIProvider
{
    OpenAI,
    Qwen,
    Custom
}

public class AIConfig
{
    public AIProvider Provider { get; set; } = AIProvider.OpenAI;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
}

public class Message
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class AIService
{
    private readonly HttpClient _httpClient;
    private AIConfig _config;

    public AIService()
    {
        _httpClient = new HttpClient();
        _config = new AIConfig();
    }

    public void Configure(AIConfig config)
    {
        _config = config;
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_config.ApiKey) && !string.IsNullOrEmpty(_config.ApiUrl);
    }

    public async Task<string> SendMessageAsync(string prompt, string systemPrompt = "")
    {
        if (!IsConfigured())
        {
            return "错误：请先在设置中配置API密钥";
        }

        var messages = new List<Dictionary<string, string>>();
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", systemPrompt }
            });
        }

        messages.Add(new Dictionary<string, string>
        {
            { "role", "user" },
            { "content", prompt }
        });

        var requestBody = new
        {
            model = _config.Model,
            messages = messages,
            temperature = _config.Temperature,
            max_tokens = _config.MaxTokens
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

        try
        {
            var response = await _httpClient.PostAsync(_config.ApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"请求失败 ({response.StatusCode}): {responseContent}";
            }

            var resultDoc = JsonDocument.Parse(responseContent);
            var choices = resultDoc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var messageObj = firstChoice.GetProperty("message");
            var assistantMessage = messageObj.GetProperty("content").GetString();

            return assistantMessage ?? "无响应";
        }
        catch (Exception ex)
        {
            return $"请求异常: {ex.Message}";
        }
    }

    public async Task<string> SendMessageAsync(List<Message> messages, string systemPrompt = "")
    {
        if (!IsConfigured())
        {
            return "错误：请先在设置中配置API密钥";
        }

        var apiMessages = new List<Dictionary<string, string>>();
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            apiMessages.Add(new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", systemPrompt }
            });
        }

        foreach (var msg in messages)
        {
            apiMessages.Add(new Dictionary<string, string>
            {
                { "role", msg.Role },
                { "content", msg.Content }
            });
        }

        var requestBody = new
        {
            model = _config.Model,
            messages = apiMessages,
            temperature = _config.Temperature,
            max_tokens = _config.MaxTokens
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

        try
        {
            var response = await _httpClient.PostAsync(_config.ApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"请求失败 ({response.StatusCode}): {responseContent}";
            }

            var resultDoc = JsonDocument.Parse(responseContent);
            var choices = resultDoc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var messageObj = firstChoice.GetProperty("message");
            var assistantMessage = messageObj.GetProperty("content").GetString();

            return assistantMessage ?? "无响应";
        }
        catch (Exception ex)
        {
            return $"请求异常: {ex.Message}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public enum ContextStrategy
{
    SlidingWindow,
    SummaryCompression,
    ImportanceBased,
    Hybrid
}

public sealed class ContextWindowManager
{
    private static readonly Lazy<ContextWindowManager> _instance = new(() => new ContextWindowManager());
    public static ContextWindowManager Instance => _instance.Value;

    private readonly TokenCounterService _tokenCounter;
    private ContextStrategy _strategy = ContextStrategy.Hybrid;
    private double _compressionRatio = 0.3;
    private int _maxContextRatio = 80;

    public event Action<int, int>? OnContextTrimmed;

    private ContextWindowManager()
    {
        _tokenCounter = TokenCounterService.Instance;
    }

    public void SetStrategy(ContextStrategy strategy)
    {
        _strategy = strategy;
    }

    public void SetCompressionRatio(double ratio)
    {
        _compressionRatio = Math.Clamp(ratio, 0.1, 0.9);
    }

    public void SetMaxContextRatio(int percentage)
    {
        _maxContextRatio = Math.Clamp(percentage, 10, 100);
    }

    public List<Message> ManageContext(List<Message> messages, int maxTokens, string systemPrompt = "")
    {
        var systemTokens = _tokenCounter.EstimateTokens(systemPrompt);
        var availableTokens = (int)((maxTokens - systemTokens) * _maxContextRatio / 100.0);

        var currentTokens = _tokenCounter.EstimateMessagesTokens(messages);

        if (currentTokens <= availableTokens)
        {
            return messages;
        }

        return _strategy switch
        {
            ContextStrategy.SlidingWindow => ApplySlidingWindow(messages, availableTokens),
            ContextStrategy.SummaryCompression => ApplySummaryCompression(messages, availableTokens),
            ContextStrategy.ImportanceBased => ApplyImportanceBased(messages, availableTokens),
            ContextStrategy.Hybrid => ApplyHybrid(messages, availableTokens),
            _ => ApplySlidingWindow(messages, availableTokens)
        };
    }

    private List<Message> ApplySlidingWindow(List<Message> messages, int maxTokens)
    {
        var result = new List<Message>();
        var pinnedMessages = messages.Where(m => m.IsPinned).ToList();
        var unpinnedMessages = messages.Where(m => !m.IsPinned).ToList();

        int pinnedTokens = _tokenCounter.EstimateMessagesTokens(pinnedMessages);
        int remainingTokens = maxTokens - pinnedTokens;

        result.AddRange(pinnedMessages);

        for (int i = unpinnedMessages.Count - 1; i >= 0; i--)
        {
            var msg = unpinnedMessages[i];
            var msgTokens = _tokenCounter.EstimateTokens(msg.Content) + 4;

            if (remainingTokens >= msgTokens)
            {
                result.Insert(result.Count - pinnedMessages.Count, msg);
                remainingTokens -= msgTokens;
            }
            else
            {
                break;
            }
        }

        OnContextTrimmed?.Invoke(messages.Count, result.Count);
        return result.OrderBy(m => m.Timestamp).ToList();
    }

    private List<Message> ApplySummaryCompression(List<Message> messages, int maxTokens)
    {
        var result = new List<Message>();
        var pinnedMessages = messages.Where(m => m.IsPinned).ToList();
        var unpinnedMessages = messages.Where(m => !m.IsPinned).ToList();

        int pinnedTokens = _tokenCounter.EstimateMessagesTokens(pinnedMessages);
        int remainingTokens = maxTokens - pinnedTokens;

        result.AddRange(pinnedMessages);

        if (unpinnedMessages.Count <= 2)
        {
            return messages;
        }

        var recentMessages = unpinnedMessages.TakeLast(4).ToList();
        var oldMessages = unpinnedMessages.Take(unpinnedMessages.Count - 4).ToList();

        int recentTokens = _tokenCounter.EstimateMessagesTokens(recentMessages);

        if (recentTokens > remainingTokens)
        {
            return ApplySlidingWindow(messages, maxTokens);
        }

        if (oldMessages.Count > 0)
        {
            var summaryContent = GenerateSummary(oldMessages);
            var summaryMessage = new Message
            {
                Role = "system",
                Content = $"[对话历史摘要]\n{summaryContent}",
                Timestamp = oldMessages.Last().Timestamp
            };

            result.Add(summaryMessage);
        }

        result.AddRange(recentMessages);

        OnContextTrimmed?.Invoke(messages.Count, result.Count);
        return result.OrderBy(m => m.Timestamp).ToList();
    }

    private List<Message> ApplyImportanceBased(List<Message> messages, int maxTokens)
    {
        var scoredMessages = messages.Select(m => new
        {
            Message = m,
            Score = CalculateImportanceScore(m, messages)
        }).OrderByDescending(x => x.Score).ToList();

        var result = new List<Message>();
        int currentTokens = 0;

        foreach (var item in scoredMessages)
        {
            var msgTokens = _tokenCounter.EstimateTokens(item.Message.Content) + 4;

            if (currentTokens + msgTokens <= maxTokens)
            {
                result.Add(item.Message);
                currentTokens += msgTokens;
            }
        }

        OnContextTrimmed?.Invoke(messages.Count, result.Count);
        return result.OrderBy(m => m.Timestamp).ToList();
    }

    private List<Message> ApplyHybrid(List<Message> messages, int maxTokens)
    {
        var pinnedMessages = messages.Where(m => m.IsPinned).ToList();
        int pinnedTokens = _tokenCounter.EstimateMessagesTokens(pinnedMessages);

        if (pinnedTokens > maxTokens * 0.5)
        {
            return ApplySlidingWindow(messages, maxTokens);
        }

        var unpinnedMessages = messages.Where(m => !m.IsPinned).ToList();
        var recentCount = Math.Min(6, unpinnedMessages.Count);
        var recentMessages = unpinnedMessages.TakeLast(recentCount).ToList();
        var oldMessages = unpinnedMessages.Take(unpinnedMessages.Count - recentCount).ToList();

        int recentTokens = _tokenCounter.EstimateMessagesTokens(recentMessages);
        int availableForOld = maxTokens - pinnedTokens - recentTokens;

        var result = new List<Message>(pinnedMessages);

        if (oldMessages.Count > 0 && availableForOld > 200)
        {
            var summaryContent = GenerateSummary(oldMessages);
            var summaryMessage = new Message
            {
                Role = "system",
                Content = $"[对话历史摘要]\n{summaryContent}",
                Timestamp = oldMessages.Last().Timestamp
            };
            result.Add(summaryMessage);
        }

        result.AddRange(recentMessages);

        OnContextTrimmed?.Invoke(messages.Count, result.Count);
        return result.OrderBy(m => m.Timestamp).ToList();
    }

    private double CalculateImportanceScore(Message message, List<Message> allMessages)
    {
        double score = 0;

        if (message.IsPinned)
            score += 100;

        if (message.Role == "user")
            score += 20;

        var recency = (DateTime.Now - message.Timestamp).TotalMinutes;
        score += Math.Max(0, 50 - recency / 10);

        var contentLength = message.Content.Length;
        if (contentLength > 500)
            score += 10;

        if (message.Content.Contains("重要") || message.Content.Contains("important"))
            score += 15;

        if (message.Content.Contains("?") || message.Content.Contains("？"))
            score += 5;

        return score;
    }

    private string GenerateSummary(List<Message> messages)
    {
        var sb = new System.Text.StringBuilder();

        var userMessages = messages.Where(m => m.Role == "user").ToList();
        var assistantMessages = messages.Where(m => m.Role == "assistant").ToList();

        if (userMessages.Count > 0)
        {
            sb.AppendLine("用户主要讨论了:");
            foreach (var msg in userMessages.Take(3))
            {
                var preview = msg.Content.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content;
                sb.AppendLine($"- {preview}");
            }
        }

        if (assistantMessages.Count > 0)
        {
            sb.AppendLine("助手提供了相关回答和帮助。");
        }

        return sb.ToString();
    }

    public ContextInfo GetContextInfo(List<Message> messages, int maxTokens)
    {
        var currentTokens = _tokenCounter.EstimateMessagesTokens(messages);
        var pinnedCount = messages.Count(m => m.IsPinned);
        var pinnedTokens = _tokenCounter.EstimateMessagesTokens(messages.Where(m => m.IsPinned).ToList());

        return new ContextInfo
        {
            TotalMessages = messages.Count,
            PinnedMessages = pinnedCount,
            CurrentTokens = currentTokens,
            MaxTokens = maxTokens,
            UsagePercentage = (double)currentTokens / maxTokens * 100,
            PinnedTokens = pinnedTokens,
            AvailableTokens = maxTokens - currentTokens,
            NeedsTrimming = currentTokens > maxTokens * _maxContextRatio / 100.0
        };
    }

    public List<Message> PrepareMessagesForRequest(
        List<Message> messages,
        string systemPrompt,
        string model,
        bool includeTools = false)
    {
        var modelInfo = ModelRouter.Instance.GetModelInfo(model);
        var maxTokens = modelInfo?.ContextWindow ?? 4096;

        var managedMessages = ManageContext(messages, maxTokens, systemPrompt);

        var result = new List<Message>();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            result.Add(new Message { Role = "system", Content = systemPrompt });
        }

        result.AddRange(managedMessages);

        return result;
    }
}

public class ContextInfo
{
    public int TotalMessages { get; set; }
    public int PinnedMessages { get; set; }
    public int CurrentTokens { get; set; }
    public int MaxTokens { get; set; }
    public double UsagePercentage { get; set; }
    public int PinnedTokens { get; set; }
    public int AvailableTokens { get; set; }
    public bool NeedsTrimming { get; set; }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public sealed class TokenCounterService
{
    private static readonly Lazy<TokenCounterService> _instance = new(() => new TokenCounterService());
    public static TokenCounterService Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, ModelPricing> _pricingTable = new();
    private readonly ConcurrentBag<UsageRecord> _usageHistory = new();
    private readonly string _usageDataPath;

    public event Action<UsageRecord>? OnUsageRecorded;

    private TokenCounterService()
    {
        _usageDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "usage_history.json");

        InitializeDefaultPricing();
        LoadUsageHistory();
    }

    private void InitializeDefaultPricing()
    {
        _pricingTable["gpt-4o"] = new ModelPricing { InputPer1K = 0.005, OutputPer1K = 0.015 };
        _pricingTable["gpt-4o-mini"] = new ModelPricing { InputPer1K = 0.00015, OutputPer1K = 0.0006 };
        _pricingTable["gpt-4-turbo"] = new ModelPricing { InputPer1K = 0.01, OutputPer1K = 0.03 };
        _pricingTable["gpt-4"] = new ModelPricing { InputPer1K = 0.03, OutputPer1K = 0.06 };
        _pricingTable["gpt-3.5-turbo"] = new ModelPricing { InputPer1K = 0.0005, OutputPer1K = 0.0015 };
        _pricingTable["claude-3-opus"] = new ModelPricing { InputPer1K = 0.015, OutputPer1K = 0.075 };
        _pricingTable["claude-3-sonnet"] = new ModelPricing { InputPer1K = 0.003, OutputPer1K = 0.015 };
        _pricingTable["claude-3-haiku"] = new ModelPricing { InputPer1K = 0.00025, OutputPer1K = 0.00125 };
        _pricingTable["qwen-turbo"] = new ModelPricing { InputPer1K = 0.0002, OutputPer1K = 0.0006 };
        _pricingTable["qwen-plus"] = new ModelPricing { InputPer1K = 0.0004, OutputPer1K = 0.0012 };
        _pricingTable["qwen-max"] = new ModelPricing { InputPer1K = 0.002, OutputPer1K = 0.006 };
        _pricingTable["deepseek-chat"] = new ModelPricing { InputPer1K = 0.0001, OutputPer1K = 0.0002 };
        _pricingTable["deepseek-coder"] = new ModelPricing { InputPer1K = 0.0001, OutputPer1K = 0.0002 };
        _pricingTable["gemini-pro"] = new ModelPricing { InputPer1K = 0.00025, OutputPer1K = 0.0005 };
        _pricingTable["gemini-1.5-pro"] = new ModelPricing { InputPer1K = 0.00125, OutputPer1K = 0.005 };
    }

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        int charCount = text.Length;
        int wordCount = 0;
        int chineseCharCount = 0;

        bool inWord = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (c >= 0x4E00 && c <= 0x9FFF)
            {
                chineseCharCount++;
                inWord = false;
            }
            else if (!inWord)
            {
                wordCount++;
                inWord = true;
            }
        }

        int tokens = (int)(wordCount * 1.3 + chineseCharCount * 1.5);
        return Math.Max(tokens, (int)(charCount / 4));
    }

    public int EstimateMessagesTokens(List<Models.Message> messages)
    {
        int total = 0;
        foreach (var msg in messages)
        {
            total += 4;
            total += EstimateTokens(msg.Role);
            total += EstimateTokens(msg.Content);
            if (msg.Images != null)
            {
                foreach (var img in msg.Images)
                {
                    total += 85;
                }
            }
        }
        total += 2;
        return total;
    }

    public double CalculateCost(string model, int inputTokens, int outputTokens)
    {
        var pricing = _pricingTable.GetValueOrDefault(model, new ModelPricing());
        return (inputTokens * pricing.InputPer1K / 1000) + (outputTokens * pricing.OutputPer1K / 1000);
    }

    public void RecordUsage(UsageRecord record)
    {
        _usageHistory.Add(record);
        OnUsageRecorded?.Invoke(record);
        _ = SaveUsageHistoryAsync();
    }

    public UsageSummary GetTodaySummary()
    {
        var today = DateTime.Today;
        var todayRecords = new List<UsageRecord>();

        foreach (var record in _usageHistory)
        {
            if (record.Timestamp.Date == today)
            {
                todayRecords.Add(record);
            }
        }

        return CreateSummary(todayRecords, "今日");
    }

    public UsageSummary GetMonthSummary()
    {
        var now = DateTime.Now;
        var monthRecords = new List<UsageRecord>();

        foreach (var record in _usageHistory)
        {
            if (record.Timestamp.Year == now.Year && record.Timestamp.Month == now.Month)
            {
                monthRecords.Add(record);
            }
        }

        return CreateSummary(monthRecords, "本月");
    }

    public UsageSummary GetTotalSummary()
    {
        return CreateSummary(_usageHistory.ToList(), "总计");
    }

    public List<UsageRecord> GetRecentUsage(int count = 100)
    {
        var result = new List<UsageRecord>();
        var temp = new List<UsageRecord>(_usageHistory);
        temp.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

        for (int i = 0; i < Math.Min(count, temp.Count); i++)
        {
            result.Add(temp[i]);
        }
        return result;
    }

    public Dictionary<string, int> GetModelUsageStats()
    {
        var stats = new Dictionary<string, int>();

        foreach (var record in _usageHistory)
        {
            if (!stats.ContainsKey(record.Model))
                stats[record.Model] = 0;
            stats[record.Model] += record.TotalTokens;
        }

        return stats;
    }

    private UsageSummary CreateSummary(List<UsageRecord> records, string period)
    {
        int totalInput = 0, totalOutput = 0;
        double totalCost = 0;
        int requestCount = 0;

        foreach (var r in records)
        {
            totalInput += r.InputTokens;
            totalOutput += r.OutputTokens;
            totalCost += r.Cost;
            requestCount++;
        }

        return new UsageSummary
        {
            Period = period,
            RequestCount = requestCount,
            TotalInputTokens = totalInput,
            TotalOutputTokens = totalOutput,
            TotalTokens = totalInput + totalOutput,
            TotalCost = totalCost
        };
    }

    private void LoadUsageHistory()
    {
        try
        {
            if (File.Exists(_usageDataPath))
            {
                var json = File.ReadAllText(_usageDataPath);
                var records = JsonSerializer.Deserialize<List<UsageRecord>>(json);
                if (records != null)
                {
                    foreach (var r in records)
                    {
                        _usageHistory.Add(r);
                    }
                }
            }
        }
        catch { }
    }

    private async Task SaveUsageHistoryAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_usageDataPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_usageHistory.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_usageDataPath, json);
        }
        catch { }
    }

    public void SetModelPricing(string model, double inputPer1K, double outputPer1K)
    {
        _pricingTable[model] = new ModelPricing
        {
            InputPer1K = inputPer1K,
            OutputPer1K = outputPer1K
        };
    }

    public ModelPricing? GetModelPricing(string model)
    {
        return _pricingTable.GetValueOrDefault(model);
    }

    public void ClearHistory()
    {
        _usageHistory.Clear();
        _ = SaveUsageHistoryAsync();
    }
}

public class ModelPricing
{
    public double InputPer1K { get; set; }
    public double OutputPer1K { get; set; }
}

public class UsageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Model { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public double Cost { get; set; }
    public string? ConversationId { get; set; }
    public string? ToolName { get; set; }
    public bool IsStreaming { get; set; }
    public TimeSpan Duration { get; set; }
}

public class UsageSummary
{
    public string Period { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
}

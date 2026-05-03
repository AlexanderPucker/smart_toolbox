using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public sealed class UsageStatisticsService
{
    private static readonly Lazy<UsageStatisticsService> _instance = new(() => new UsageStatisticsService());
    public static UsageStatisticsService Instance => _instance.Value;

    private readonly TokenCounterService _tokenCounter;
    private readonly string _budgetConfigPath;

    private double _dailyBudget = 0;
    private double _monthlyBudget = 0;
    private bool _alertOnBudgetExceeded = true;

    public event Action<double, double>? OnDailyBudgetWarning;
    public event Action<double, double>? OnMonthlyBudgetWarning;

    private UsageStatisticsService()
    {
        _tokenCounter = TokenCounterService.Instance;
        _budgetConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "budget_config.json");

        LoadBudgetConfig();
        _tokenCounter.OnUsageRecorded += OnUsageRecorded;
    }

    public void SetDailyBudget(double budget)
    {
        _dailyBudget = budget;
        SaveBudgetConfig();
    }

    public void SetMonthlyBudget(double budget)
    {
        _monthlyBudget = budget;
        SaveBudgetConfig();
    }

    public void SetAlertOnBudgetExceeded(bool alert)
    {
        _alertOnBudgetExceeded = alert;
        SaveBudgetConfig();
    }

    public UsageDashboard GetDashboard()
    {
        var today = _tokenCounter.GetTodaySummary();
        var month = _tokenCounter.GetMonthSummary();
        var total = _tokenCounter.GetTotalSummary();
        var recentUsage = _tokenCounter.GetRecentUsage(20);
        var modelStats = _tokenCounter.GetModelUsageStats();

        return new UsageDashboard
        {
            Today = today,
            Month = month,
            Total = total,
            RecentUsage = recentUsage,
            ModelStats = modelStats,
            DailyBudget = _dailyBudget,
            MonthlyBudget = _monthlyBudget,
            DailyBudgetUsage = _dailyBudget > 0 ? today.TotalCost / _dailyBudget : 0,
            MonthlyBudgetUsage = _monthlyBudget > 0 ? month.TotalCost / _monthlyBudget : 0
        };
    }

    public List<DailyUsage> GetDailyUsageHistory(int days = 30)
    {
        var history = new List<DailyUsage>();
        var recentUsage = _tokenCounter.GetRecentUsage(1000);

        var groupedByDay = recentUsage
            .GroupBy(r => r.Timestamp.Date)
            .OrderByDescending(g => g.Key)
            .Take(days);

        foreach (var day in groupedByDay)
        {
            var records = day.ToList();
            history.Add(new DailyUsage
            {
                Date = day.Key,
                RequestCount = records.Count,
                TotalTokens = records.Sum(r => r.TotalTokens),
                TotalCost = records.Sum(r => r.Cost),
                ModelBreakdown = records
                    .GroupBy(r => r.Model)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalTokens))
            });
        }

        return history;
    }

    public List<HourlyUsage> GetHourlyUsageToday()
    {
        var today = DateTime.Today;
        var recentUsage = _tokenCounter.GetRecentUsage(1000);

        var todayRecords = recentUsage.Where(r => r.Timestamp.Date == today);

        var groupedByHour = todayRecords
            .GroupBy(r => r.Timestamp.Hour)
            .OrderBy(g => g.Key);

        var result = new List<HourlyUsage>();
        for (int hour = 0; hour < 24; hour++)
        {
            var hourRecords = todayRecords.Where(r => r.Timestamp.Hour == hour).ToList();
            result.Add(new HourlyUsage
            {
                Hour = hour,
                RequestCount = hourRecords.Count,
                TotalTokens = hourRecords.Sum(r => r.TotalTokens),
                TotalCost = hourRecords.Sum(r => r.Cost)
            });
        }

        return result;
    }

    public CostProjection GetCostProjection()
    {
        var today = _tokenCounter.GetTodaySummary();
        var month = _tokenCounter.GetMonthSummary();

        var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
        var daysPassed = DateTime.Now.Day;
        var daysRemaining = daysInMonth - daysPassed;

        var dailyAverage = daysPassed > 0 ? month.TotalCost / daysPassed : 0;
        var projectedMonthlyCost = dailyAverage * daysInMonth;
        var projectedEndOfMonth = month.TotalCost + (dailyAverage * daysRemaining);

        return new CostProjection
        {
            DailyAverage = dailyAverage,
            ProjectedMonthlyCost = projectedMonthlyCost,
            ProjectedEndOfMonth = projectedEndOfMonth,
            CurrentMonthCost = month.TotalCost,
            DaysRemaining = daysRemaining,
            MonthlyBudget = _monthlyBudget,
            BudgetRemaining = _monthlyBudget > 0 ? Math.Max(0, _monthlyBudget - month.TotalCost) : 0,
            OnTrack = _monthlyBudget <= 0 || projectedEndOfMonth <= _monthlyBudget
        };
    }

    public List<ModelUsageDetail> GetModelUsageDetails()
    {
        var recentUsage = _tokenCounter.GetRecentUsage(500);
        var modelGroups = recentUsage.GroupBy(r => r.Model);

        return modelGroups.Select(g =>
        {
            var records = g.ToList();
            var pricing = _tokenCounter.GetModelPricing(g.Key);

            return new ModelUsageDetail
            {
                Model = g.Key,
                RequestCount = records.Count,
                TotalInputTokens = records.Sum(r => r.InputTokens),
                TotalOutputTokens = records.Sum(r => r.OutputTokens),
                TotalTokens = records.Sum(r => r.TotalTokens),
                TotalCost = records.Sum(r => r.Cost),
                AverageTokensPerRequest = records.Count > 0 ? records.Average(r => r.TotalTokens) : 0,
                AverageCostPerRequest = records.Count > 0 ? records.Average(r => r.Cost) : 0,
                InputPricePer1K = pricing?.InputPer1K ?? 0,
                OutputPricePer1K = pricing?.OutputPer1K ?? 0
            };
        }).OrderByDescending(m => m.TotalCost).ToList();
    }

    public ToolUsageStats GetToolUsageStats()
    {
        var recentUsage = _tokenCounter.GetRecentUsage(500);
        var toolGroups = recentUsage
            .Where(r => !string.IsNullOrEmpty(r.ToolName))
            .GroupBy(r => r.ToolName!);

        return new ToolUsageStats
        {
            ToolUsage = toolGroups.ToDictionary(
                g => g.Key,
                g => new ToolUsageDetail
                {
                    ToolName = g.Key,
                    UsageCount = g.Count(),
                    TotalTokens = g.Sum(r => r.TotalTokens),
                    TotalCost = g.Sum(r => r.Cost)
                }
            )
        };
    }

    private void OnUsageRecorded(UsageRecord record)
    {
        if (!_alertOnBudgetExceeded)
            return;

        if (_dailyBudget > 0)
        {
            var todayCost = _tokenCounter.GetTodaySummary().TotalCost;
            if (todayCost >= _dailyBudget * 0.8)
            {
                OnDailyBudgetWarning?.Invoke(todayCost, _dailyBudget);
            }
        }

        if (_monthlyBudget > 0)
        {
            var monthCost = _tokenCounter.GetMonthSummary().TotalCost;
            if (monthCost >= _monthlyBudget * 0.8)
            {
                OnMonthlyBudgetWarning?.Invoke(monthCost, _monthlyBudget);
            }
        }
    }

    private void LoadBudgetConfig()
    {
        try
        {
            if (File.Exists(_budgetConfigPath))
            {
                var json = File.ReadAllText(_budgetConfigPath);
                var config = JsonSerializer.Deserialize<BudgetConfig>(json);
                if (config != null)
                {
                    _dailyBudget = config.DailyBudget;
                    _monthlyBudget = config.MonthlyBudget;
                    _alertOnBudgetExceeded = config.AlertOnBudgetExceeded;
                }
            }
        }
        catch { }
    }

    private void SaveBudgetConfig()
    {
        try
        {
            var config = new BudgetConfig
            {
                DailyBudget = _dailyBudget,
                MonthlyBudget = _monthlyBudget,
                AlertOnBudgetExceeded = _alertOnBudgetExceeded
            };

            var directory = Path.GetDirectoryName(_budgetConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_budgetConfigPath, json);
        }
        catch { }
    }

    public void ExportUsageReport(string outputPath, string format = "json")
    {
        var dashboard = GetDashboard();
        var history = GetDailyUsageHistory(30);
        var modelDetails = GetModelUsageDetails();

        var report = new UsageReport
        {
            GeneratedAt = DateTime.Now,
            Dashboard = dashboard,
            DailyHistory = history,
            ModelDetails = modelDetails
        };

        var content = format.ToLower() switch
        {
            "json" => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            _ => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        };

        File.WriteAllText(outputPath, content);
    }
}

public class BudgetConfig
{
    public double DailyBudget { get; set; }
    public double MonthlyBudget { get; set; }
    public bool AlertOnBudgetExceeded { get; set; } = true;
}

public class UsageDashboard
{
    public UsageSummary Today { get; set; } = new();
    public UsageSummary Month { get; set; } = new();
    public UsageSummary Total { get; set; } = new();
    public List<UsageRecord> RecentUsage { get; set; } = new();
    public Dictionary<string, int> ModelStats { get; set; } = new();
    public double DailyBudget { get; set; }
    public double MonthlyBudget { get; set; }
    public double DailyBudgetUsage { get; set; }
    public double MonthlyBudgetUsage { get; set; }
}

public class DailyUsage
{
    public DateTime Date { get; set; }
    public int RequestCount { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public Dictionary<string, int> ModelBreakdown { get; set; } = new();
}

public class HourlyUsage
{
    public int Hour { get; set; }
    public int RequestCount { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
}

public class CostProjection
{
    public double DailyAverage { get; set; }
    public double ProjectedMonthlyCost { get; set; }
    public double ProjectedEndOfMonth { get; set; }
    public double CurrentMonthCost { get; set; }
    public int DaysRemaining { get; set; }
    public double MonthlyBudget { get; set; }
    public double BudgetRemaining { get; set; }
    public bool OnTrack { get; set; }
}

public class ModelUsageDetail
{
    public string Model { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public double AverageTokensPerRequest { get; set; }
    public double AverageCostPerRequest { get; set; }
    public double InputPricePer1K { get; set; }
    public double OutputPricePer1K { get; set; }
}

public class ToolUsageStats
{
    public Dictionary<string, ToolUsageDetail> ToolUsage { get; set; } = new();
}

public class ToolUsageDetail
{
    public string ToolName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
}

public class UsageReport
{
    public DateTime GeneratedAt { get; set; }
    public UsageDashboard Dashboard { get; set; } = new();
    public List<DailyUsage> DailyHistory { get; set; } = new();
    public List<ModelUsageDetail> ModelDetails { get; set; } = new();
}

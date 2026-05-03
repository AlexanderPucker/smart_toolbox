using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class UsageStatsViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _todayCost;

    [ObservableProperty]
    private double _monthCost;

    [ObservableProperty]
    private double _totalCost;

    [ObservableProperty]
    private int _todayRequests;

    [ObservableProperty]
    private int _monthRequests;

    [ObservableProperty]
    private int _totalRequests;

    [ObservableProperty]
    private int _todayTokens;

    [ObservableProperty]
    private int _monthTokens;

    [ObservableProperty]
    private int _totalTokens;

    [ObservableProperty]
    private double _dailyBudget;

    [ObservableProperty]
    private double _monthlyBudget;

    [ObservableProperty]
    private double _dailyBudgetUsagePercent;

    [ObservableProperty]
    private double _monthlyBudgetUsagePercent;

    [ObservableProperty]
    private string _budgetStatus = "正常";

    [ObservableProperty]
    private string _projectionText = "";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<ModelUsageItem> ModelUsages { get; } = new();
    public ObservableCollection<DailyUsageItem> DailyHistory { get; } = new();
    public ObservableCollection<RecentRequestItem> RecentRequests { get; } = new();

    private readonly UsageStatisticsService _statsService;
    private readonly TokenCounterService _tokenCounter;

    public UsageStatsViewModel()
    {
        _statsService = UsageStatisticsService.Instance;
        _tokenCounter = TokenCounterService.Instance;
        _tokenCounter.OnUsageRecorded += OnUsageRecorded;
        LoadData();
    }

    private void OnUsageRecorded(UsageRecord record)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => LoadData());
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadData();
    }

    private void LoadData()
    {
        IsLoading = true;

        try
        {
            var dashboard = _statsService.GetDashboard();

            TodayCost = dashboard.Today.TotalCost;
            MonthCost = dashboard.Month.TotalCost;
            TotalCost = dashboard.Total.TotalCost;

            TodayRequests = dashboard.Today.RequestCount;
            MonthRequests = dashboard.Month.RequestCount;
            TotalRequests = dashboard.Total.RequestCount;

            TodayTokens = dashboard.Today.TotalTokens;
            MonthTokens = dashboard.Month.TotalTokens;
            TotalTokens = dashboard.Total.TotalTokens;

            DailyBudget = dashboard.DailyBudget;
            MonthlyBudget = dashboard.MonthlyBudget;

            DailyBudgetUsagePercent = dashboard.DailyBudgetUsage * 100;
            MonthlyBudgetUsagePercent = dashboard.MonthlyBudgetUsage * 100;

            if (DailyBudgetUsagePercent > 80 || MonthlyBudgetUsagePercent > 80)
            {
                BudgetStatus = "⚠️ 预算警告";
            }
            else if (DailyBudgetUsagePercent > 100 || MonthlyBudgetUsagePercent > 100)
            {
                BudgetStatus = "🔴 超出预算";
            }
            else
            {
                BudgetStatus = "✅ 正常";
            }

            var projection = _statsService.GetCostProjection();
            ProjectionText = $"预计本月: ${projection.ProjectedEndOfMonth:F2} | " +
                            (projection.OnTrack ? "✅ 在预算内" : "⚠️ 可能超支");

            ModelUsages.Clear();
            var modelDetails = _statsService.GetModelUsageDetails();
            foreach (var detail in modelDetails)
            {
                ModelUsages.Add(new ModelUsageItem
                {
                    Model = detail.Model,
                    RequestCount = detail.RequestCount,
                    TotalTokens = detail.TotalTokens,
                    TotalCost = detail.TotalCost,
                    Percentage = TotalCost > 0 ? detail.TotalCost / TotalCost * 100 : 0
                });
            }

            DailyHistory.Clear();
            var history = _statsService.GetDailyUsageHistory(7);
            foreach (var day in history)
            {
                DailyHistory.Add(new DailyUsageItem
                {
                    Date = day.Date.ToString("MM-dd"),
                    RequestCount = day.RequestCount,
                    TotalTokens = day.TotalTokens,
                    TotalCost = day.TotalCost
                });
            }

            RecentRequests.Clear();
            var recent = _tokenCounter.GetRecentUsage(10);
            foreach (var record in recent)
            {
                RecentRequests.Add(new RecentRequestItem
                {
                    Time = record.Timestamp.ToString("HH:mm:ss"),
                    Model = record.Model,
                    Tokens = record.TotalTokens,
                    Cost = record.Cost
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetDailyBudget(double budget)
    {
        _statsService.SetDailyBudget(budget);
        DailyBudget = budget;
    }

    [RelayCommand]
    private void SetMonthlyBudget(double budget)
    {
        _statsService.SetMonthlyBudget(budget);
        MonthlyBudget = budget;
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _tokenCounter.ClearHistory();
        LoadData();
    }

    [RelayCommand]
    private void ExportReport()
    {
    }
}

public class ModelUsageItem
{
    public string Model { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public double Percentage { get; set; }
}

public class DailyUsageItem
{
    public string Date { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
}

public class RecentRequestItem
{
    public string Time { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Tokens { get; set; }
    public double Cost { get; set; }
}

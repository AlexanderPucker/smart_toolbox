using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace SmartToolbox.ViewModels;

public partial class TimestampViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _timestampInput = "";

    [ObservableProperty]
    private string _dateTimeInput = "";

    [ObservableProperty]
    private string _currentTimestamp = "";

    [ObservableProperty]
    private string _currentDateTime = "";

    [ObservableProperty]
    private string _convertedResult = "";

    [ObservableProperty]
    private string _statusMessage = "Unix 时间戳与日期时间互转";

    [ObservableProperty]
    private bool _isMilliseconds = true;

    private readonly System.Threading.Timer _timer;

    public TimestampViewModel()
    {
        UpdateCurrentTime();
        _timer = new System.Threading.Timer(_ => UpdateCurrentTime(), null, 0, 1000);
    }

    private void UpdateCurrentTime()
    {
        var now = DateTime.UtcNow;
        var unixMs = new DateTimeOffset(now).ToUnixTimeMilliseconds();
        var unixSec = new DateTimeOffset(now).ToUnixTimeSeconds();
        CurrentTimestamp = IsMilliseconds ? unixMs.ToString() : unixSec.ToString();
        CurrentDateTime = now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }

    partial void OnIsMillisecondsChanged(bool value) => UpdateCurrentTime();

    [RelayCommand]
    private void TimestampToDate()
    {
        if (string.IsNullOrWhiteSpace(TimestampInput))
        {
            StatusMessage = "请输入时间戳";
            return;
        }

        try
        {
            long ts = long.Parse(TimestampInput.Trim());
            DateTime dt;
            if (IsMilliseconds || ts > 9999999999L)
                dt = DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime;
            else
                dt = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;

            ConvertedResult = dt.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                $"\n星期{GetChineseWeekday(dt.DayOfWeek)}" +
                $"\n{dt:yyyy年MM月dd日 HH时mm分ss秒}";
            StatusMessage = "转换成功";
        }
        catch (Exception ex)
        {
            ConvertedResult = "";
            StatusMessage = $"转换失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DateToTimestamp()
    {
        if (string.IsNullOrWhiteSpace(DateTimeInput))
        {
            StatusMessage = "请输入日期时间";
            return;
        }

        try
        {
            var dt = DateTime.Parse(DateTimeInput.Trim());
            var offset = new DateTimeOffset(dt);
            ConvertedResult = IsMilliseconds
                ? $"毫秒: {offset.ToUnixTimeMilliseconds()}\n秒: {offset.ToUnixTimeSeconds()}"
                : $"秒: {offset.ToUnixTimeSeconds()}\n毫秒: {offset.ToUnixTimeMilliseconds()}";
            StatusMessage = "转换成功";
        }
        catch (Exception ex)
        {
            ConvertedResult = "";
            StatusMessage = $"转换失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UseCurrentTime()
    {
        TimestampInput = CurrentTimestamp;
        DateTimeInput = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        StatusMessage = "已填入当前时间";
    }

    [RelayCommand]
    private void Clear()
    {
        TimestampInput = "";
        DateTimeInput = "";
        ConvertedResult = "";
        StatusMessage = "已清空";
    }

    private static string GetChineseWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "一",
        DayOfWeek.Tuesday => "二",
        DayOfWeek.Wednesday => "三",
        DayOfWeek.Thursday => "四",
        DayOfWeek.Friday => "五",
        DayOfWeek.Saturday => "六",
        DayOfWeek.Sunday => "日",
        _ => ""
    };

    public void Dispose() => _timer.Dispose();
}

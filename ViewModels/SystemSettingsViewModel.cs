using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SmartToolbox.ViewModels;

/// <summary>
/// 系统设置视图模型类
/// 负责管理系统设置界面的数据和操作
/// </summary>
public partial class SystemSettingsViewModel : ViewModelBase
{
    /// <summary>
    /// 应用程序主题（浅色/深色）
    /// </summary>
    [ObservableProperty]
    private string _appTheme = "浅色";

    /// <summary>
    /// 应用程序语言
    /// </summary>
    [ObservableProperty]
    private string _language = "中文";

    /// <summary>
    /// 是否开机启动
    /// </summary>
    [ObservableProperty]
    private bool _startWithSystem = false;

    /// <summary>
    /// 是否最小化到托盘
    /// </summary>
    [ObservableProperty]
    private bool _minimizeToTray = true;

    /// <summary>
    /// 是否自动检查更新
    /// </summary>
    [ObservableProperty]
    private bool _checkUpdates = true;

    /// <summary>
    /// 是否自动保存设置
    /// </summary>
    [ObservableProperty]
    private bool _autoSave = false;

    /// <summary>
    /// 构造函数
    /// 初始化系统设置视图模型
    /// </summary>
    public SystemSettingsViewModel()
    {
        // 初始化设置，可以从配置文件加载
        LoadSettings();
    }

    /// <summary>
    /// 加载设置
    /// 从配置文件加载用户设置（目前使用默认值）
    /// </summary>
    private void LoadSettings()
    {
        // 这里可以从配置文件加载设置
        // 目前使用默认值
    }

    /// <summary>
    /// 保存设置命令
    /// 将当前设置保存到配置文件
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        // 保存设置到配置文件
        // 可以显示一个保存成功的消息
    }

    /// <summary>
    /// 重置设置命令
    /// 将所有设置重置为默认值
    /// </summary>
    [RelayCommand]
    private void ResetSettings()
    {
        // 重置为默认设置
        AppTheme = "浅色";
        Language = "中文";
        StartWithSystem = false;
        MinimizeToTray = true;
        CheckUpdates = true;
        AutoSave = false;
    }
}
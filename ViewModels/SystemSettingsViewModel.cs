using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace SmartToolbox.ViewModels;

/// <summary>
/// 系统设置视图模型类
/// 负责管理系统设置界面的数据和操作
/// </summary>
public partial class SystemSettingsViewModel : ViewModelBase
{
    #region Tab管理
    /// <summary>
    /// 当前选中的Tab索引
    /// </summary>
    [ObservableProperty]
    private int _selectedTabIndex = 0;
    #endregion

    #region 外观设置
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
    #endregion

    #region 启动行为
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
    #endregion

    #region 其他设置
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
    #endregion

    #region 文件处理设置
    /// <summary>
    /// 默认保存路径
    /// </summary>
    [ObservableProperty]
    private string _defaultSavePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    /// <summary>
    /// 文件命名规则
    /// </summary>
    [ObservableProperty]
    private string _fileNamingRule = "原文件名_时间戳";

    /// <summary>
    /// 是否创建备份
    /// </summary>
    [ObservableProperty]
    private bool _createBackup = true;

    /// <summary>
    /// 备份文件数量
    /// </summary>
    [ObservableProperty]
    private int _backupFileCount = 5;
    #endregion

    #region 高级设置
    /// <summary>
    /// 是否启用日志记录
    /// </summary>
    [ObservableProperty]
    private bool _enableLogging = true;

    /// <summary>
    /// 日志级别
    /// </summary>
    [ObservableProperty]
    private string _logLevel = "信息";

    /// <summary>
    /// 是否启用调试模式
    /// </summary>
    [ObservableProperty]
    private bool _enableDebugMode = false;

    /// <summary>
    /// 是否启用实验性功能
    /// </summary>
    [ObservableProperty]
    private bool _enableExperimentalFeatures = false;
    #endregion

    #region 关于信息
    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName => "Smart Toolbox";

    /// <summary>
    /// 应用版本
    /// </summary>
    public string AppVersion => "1.0.0";

    /// <summary>
    /// 版权信息
    /// </summary>
    public string Copyright => "© 2024 Smart Toolbox. All rights reserved.";

    /// <summary>
    /// 开源许可证
    /// </summary>
    public string License => "MIT License";

    /// <summary>
    /// GitHub链接
    /// </summary>
    public string GitHubUrl => "https://github.com/your-username/smart-toolbox";
    #endregion

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
        
        // 重置文件处理设置
        DefaultSavePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        FileNamingRule = "原文件名_时间戳";
        CreateBackup = true;
        BackupFileCount = 5;
        
        // 重置高级设置
        EnableLogging = true;
        LogLevel = "信息";
        EnableDebugMode = false;
        EnableExperimentalFeatures = false;
    }

    /// <summary>
    /// 选择保存路径命令
    /// 打开文件夹选择对话框
    /// </summary>
    [RelayCommand]
    private void SelectSavePath()
    {
        try
        {
            // 这里应该使用Avalonia的文件夹选择对话框
            // 暂时使用Documents文件夹作为示例
            DefaultSavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        catch (Exception ex)
        {
            // 处理异常，可以记录日志或显示错误消息
            System.Diagnostics.Debug.WriteLine($"无法选择保存路径: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开GitHub链接命令
    /// 在默认浏览器中打开GitHub页面
    /// </summary>
    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            var uri = new Uri(GitHubUrl);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // 处理异常，可以记录日志或显示错误消息
            System.Diagnostics.Debug.WriteLine($"无法打开GitHub链接: {ex.Message}");
        }
    }
}
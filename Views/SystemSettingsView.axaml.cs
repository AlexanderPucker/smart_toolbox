using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SmartToolbox.Models;
using SmartToolbox.ViewModels;
using System;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

/// <summary>
/// 系统设置视图类
/// 提供应用程序设置选项的用户控件，使用代码后台管理控件
/// </summary>
public partial class SystemSettingsView : UserControl
{
    private readonly SystemSettingsViewModel _viewModel;

    /// <summary>
    /// 系统设置视图构造函数
    /// 初始化组件和相关控件
    /// </summary>
    public SystemSettingsView()
    {
        InitializeComponent();
        
        // 创建并设置ViewModel
        _viewModel = new SystemSettingsViewModel();
        
        // 初始化控件
        InitializeControls();
    }

    /// <summary>
    /// 初始化控件
    /// 设置控件的初始值和事件处理程序
    /// </summary>
    private void InitializeControls()
    {
        // 设置标题
        var appNameText = this.FindControl<TextBlock>("AppNameText");
        if (appNameText != null)
        {
            appNameText.Text = _viewModel.AppName;
        }

        var appVersionText = this.FindControl<TextBlock>("AppVersionText");
        if (appVersionText != null)
        {
            appVersionText.Text = $"版本 {_viewModel.AppVersion}";
        }

        var appDescriptionText = this.FindControl<TextBlock>("AppDescriptionText");
        if (appDescriptionText != null)
        {
            appDescriptionText.Text = "一个智能的工具箱应用程序，提供文件管理、批量操作等功能";
        }

        var copyrightText = this.FindControl<TextBlock>("CopyrightText");
        if (copyrightText != null)
        {
            copyrightText.Text = _viewModel.Copyright;
        }

        var licenseText = this.FindControl<TextBlock>("LicenseText");
        if (licenseText != null)
        {
            licenseText.Text = $"开源许可证: {_viewModel.License}";
        }

        // 初始化外观设置
        InitializeAppearanceSettings();

        // 初始化启动行为设置
        InitializeStartupBehaviorSettings();

        // 初始化其他设置
        InitializeOtherSettings();

        // 初始化文件处理设置
        InitializeFileHandlingSettings();

        // 初始化高级设置
        InitializeAdvancedSettings();

        // 绑定按钮事件
        BindButtonEvents();
    }

    /// <summary>
    /// 初始化外观设置
    /// 设置主题和语言下拉框的选项和初始值
    /// </summary>
    private void InitializeAppearanceSettings()
    {
        var themeComboBox = this.FindControl<ComboBox>("ThemeComboBox");
        if (themeComboBox != null)
        {
            themeComboBox.ItemsSource = SystemSettingsConfig.ThemeOptions;
            themeComboBox.SelectedIndex = GetThemeIndex(_viewModel.AppTheme);
            themeComboBox.SelectionChanged += (sender, e) =>
            {
                if (themeComboBox.SelectedItem is SystemSettingsItem selectedItem)
                {
                    _viewModel.AppTheme = selectedItem.DisplayName;
                }
            };
        }

        var languageComboBox = this.FindControl<ComboBox>("LanguageComboBox");
        if (languageComboBox != null)
        {
            languageComboBox.ItemsSource = SystemSettingsConfig.LanguageOptions;
            languageComboBox.SelectedIndex = GetLanguageIndex(_viewModel.Language);
            languageComboBox.SelectionChanged += (sender, e) =>
            {
                if (languageComboBox.SelectedItem is SystemSettingsItem selectedItem)
                {
                    _viewModel.Language = selectedItem.DisplayName;
                }
            };
        }
    }

    /// <summary>
    /// 初始化启动行为设置
    /// 设置启动行为复选框的初始状态和事件处理
    /// </summary>
    private void InitializeStartupBehaviorSettings()
    {
        var startWithSystemCheckBox = this.FindControl<CheckBox>("StartWithSystemCheckBox");
        if (startWithSystemCheckBox != null)
        {
            startWithSystemCheckBox.IsChecked = _viewModel.StartWithSystem;
            startWithSystemCheckBox.Click += (sender, e) =>
            {
                _viewModel.StartWithSystem = startWithSystemCheckBox.IsChecked ?? false;
            };
        }

        var minimizeToTrayCheckBox = this.FindControl<CheckBox>("MinimizeToTrayCheckBox");
        if (minimizeToTrayCheckBox != null)
        {
            minimizeToTrayCheckBox.IsChecked = _viewModel.MinimizeToTray;
            minimizeToTrayCheckBox.Click += (sender, e) =>
            {
                _viewModel.MinimizeToTray = minimizeToTrayCheckBox.IsChecked ?? false;
            };
        }
    }

    /// <summary>
    /// 初始化其他设置
    /// 设置其他选项复选框的初始状态和事件处理
    /// </summary>
    private void InitializeOtherSettings()
    {
        var checkUpdatesCheckBox = this.FindControl<CheckBox>("CheckUpdatesCheckBox");
        if (checkUpdatesCheckBox != null)
        {
            checkUpdatesCheckBox.IsChecked = _viewModel.CheckUpdates;
            checkUpdatesCheckBox.Click += (sender, e) =>
            {
                _viewModel.CheckUpdates = checkUpdatesCheckBox.IsChecked ?? false;
            };
        }

        var autoSaveCheckBox = this.FindControl<CheckBox>("AutoSaveCheckBox");
        if (autoSaveCheckBox != null)
        {
            autoSaveCheckBox.IsChecked = _viewModel.AutoSave;
            autoSaveCheckBox.Click += (sender, e) =>
            {
                _viewModel.AutoSave = autoSaveCheckBox.IsChecked ?? false;
            };
        }
    }

    /// <summary>
    /// 初始化文件处理设置
    /// 设置文件处理相关控件的初始值和事件处理
    /// </summary>
    private void InitializeFileHandlingSettings()
    {
        var defaultSavePathTextBox = this.FindControl<TextBox>("DefaultSavePathTextBox");
        if (defaultSavePathTextBox != null)
        {
            defaultSavePathTextBox.Text = _viewModel.DefaultSavePath;
        }

        var browseSavePathButton = this.FindControl<Button>("BrowseSavePathButton");
        if (browseSavePathButton != null)
        {
            browseSavePathButton.Click += SelectSavePath_Click;
        }

        var fileNamingRuleComboBox = this.FindControl<ComboBox>("FileNamingRuleComboBox");
        if (fileNamingRuleComboBox != null)
        {
            fileNamingRuleComboBox.ItemsSource = SystemSettingsConfig.FileNamingRuleOptions;
            fileNamingRuleComboBox.SelectedIndex = GetNamingRuleIndex(_viewModel.FileNamingRule);
            fileNamingRuleComboBox.SelectionChanged += (sender, e) =>
            {
                if (fileNamingRuleComboBox.SelectedItem is SystemSettingsItem selectedItem)
                {
                    _viewModel.FileNamingRule = selectedItem.DisplayName;
                }
            };
        }

        var createBackupCheckBox = this.FindControl<CheckBox>("CreateBackupCheckBox");
        if (createBackupCheckBox != null)
        {
            createBackupCheckBox.IsChecked = _viewModel.CreateBackup;
            createBackupCheckBox.Click += (sender, e) =>
            {
                _viewModel.CreateBackup = createBackupCheckBox.IsChecked ?? false;
            };
        }

        var backupFileCountNumeric = this.FindControl<NumericUpDown>("BackupFileCountNumeric");
        if (backupFileCountNumeric != null)
        {
            backupFileCountNumeric.Value = _viewModel.BackupFileCount;
            backupFileCountNumeric.ValueChanged += (sender, e) =>
            {
                _viewModel.BackupFileCount = (int)(backupFileCountNumeric.Value);
            };
        }
    }

    /// <summary>
    /// 初始化高级设置
    /// 设置高级选项控件的初始值和事件处理
    /// </summary>
    private void InitializeAdvancedSettings()
    {
        var enableLoggingCheckBox = this.FindControl<CheckBox>("EnableLoggingCheckBox");
        if (enableLoggingCheckBox != null)
        {
            enableLoggingCheckBox.IsChecked = _viewModel.EnableLogging;
            enableLoggingCheckBox.Click += (sender, e) =>
            {
                _viewModel.EnableLogging = enableLoggingCheckBox.IsChecked ?? false;
            };
        }

        var logLevelComboBox = this.FindControl<ComboBox>("LogLevelComboBox");
        if (logLevelComboBox != null)
        {
            logLevelComboBox.ItemsSource = SystemSettingsConfig.LogLevelOptions;
            logLevelComboBox.SelectedIndex = GetLogLevelIndex(_viewModel.LogLevel);
            logLevelComboBox.SelectionChanged += (sender, e) =>
            {
                if (logLevelComboBox.SelectedItem is SystemSettingsItem selectedItem)
                {
                    _viewModel.LogLevel = selectedItem.DisplayName;
                }
            };
        }

        var enableDebugModeCheckBox = this.FindControl<CheckBox>("EnableDebugModeCheckBox");
        if (enableDebugModeCheckBox != null)
        {
            enableDebugModeCheckBox.IsChecked = _viewModel.EnableDebugMode;
            enableDebugModeCheckBox.Click += (sender, e) =>
            {
                _viewModel.EnableDebugMode = enableDebugModeCheckBox.IsChecked ?? false;
            };
        }

        var enableExperimentalFeaturesCheckBox = this.FindControl<CheckBox>("EnableExperimentalFeaturesCheckBox");
        if (enableExperimentalFeaturesCheckBox != null)
        {
            enableExperimentalFeaturesCheckBox.IsChecked = _viewModel.EnableExperimentalFeatures;
            enableExperimentalFeaturesCheckBox.Click += (sender, e) =>
            {
                _viewModel.EnableExperimentalFeatures = enableExperimentalFeaturesCheckBox.IsChecked ?? false;
            };
        }
    }

    /// <summary>
    /// 绑定按钮事件
    /// 为操作按钮绑定事件处理程序
    /// </summary>
    private void BindButtonEvents()
    {
        var resetSettingsButton = this.FindControl<Button>("ResetSettingsButton");
        if (resetSettingsButton != null)
        {
            resetSettingsButton.Click += ResetSettings_Click;
        }

        var saveSettingsButton = this.FindControl<Button>("SaveSettingsButton");
        if (saveSettingsButton != null)
        {
            saveSettingsButton.Click += SaveSettings_Click;
        }

        var openGitHubButton = this.FindControl<Button>("OpenGitHubButton");
        if (openGitHubButton != null)
        {
            openGitHubButton.Click += OpenGitHub_Click;
        }
    }

    /// <summary>
    /// 选择保存路径事件处理方法
    /// 异步调用选择保存路径功能
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">路由事件参数</param>
    private async void SelectSavePath_Click(object? sender, RoutedEventArgs e)
    {
        await SelectSavePathAsync();
    }

    /// <summary>
    /// 异步选择保存路径方法
    /// 使用文件系统选择器让用户选择保存文件夹
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task SelectSavePathAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        try
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择默认保存路径",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var savePathTextBox = this.FindControl<TextBox>("DefaultSavePathTextBox");
                if (savePathTextBox != null)
                {
                    var newPath = folders[0].Path.LocalPath;
                    savePathTextBox.Text = newPath;
                    _viewModel.DefaultSavePath = newPath;
                }
            }
        }
        catch (Exception ex)
        {
            // 在实际应用中，可以显示错误消息
            System.Diagnostics.Debug.WriteLine($"选择保存路径失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 重置设置事件处理方法
    /// 重置所有设置为默认值
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">路由事件参数</param>
    private void ResetSettings_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.ResetSettingsCommand.Execute(null);
        
        // 更新界面控件
        UpdateUIFromViewModel();
    }

    /// <summary>
    /// 保存设置事件处理方法
    /// 保存当前设置
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">路由事件参数</param>
    private void SaveSettings_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.SaveSettingsCommand.Execute(null);
    }

    /// <summary>
    /// 打开GitHub页面事件处理方法
    /// 打开项目GitHub页面
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">路由事件参数</param>
    private void OpenGitHub_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.OpenGitHubCommand.Execute(null);
    }

    /// <summary>
    /// 从ViewModel更新UI控件
    /// 当设置重置时同步UI显示
    /// </summary>
    private void UpdateUIFromViewModel()
    {
        // 更新主题下拉框
        var themeComboBox = this.FindControl<ComboBox>("ThemeComboBox");
        if (themeComboBox != null)
        {
            themeComboBox.SelectedIndex = GetThemeIndex(_viewModel.AppTheme);
        }

        // 更新语言下拉框
        var languageComboBox = this.FindControl<ComboBox>("LanguageComboBox");
        if (languageComboBox != null)
        {
            languageComboBox.SelectedIndex = GetLanguageIndex(_viewModel.Language);
        }

        // 更新启动行为复选框
        var startWithSystemCheckBox = this.FindControl<CheckBox>("StartWithSystemCheckBox");
        if (startWithSystemCheckBox != null)
        {
            startWithSystemCheckBox.IsChecked = _viewModel.StartWithSystem;
        }

        var minimizeToTrayCheckBox = this.FindControl<CheckBox>("MinimizeToTrayCheckBox");
        if (minimizeToTrayCheckBox != null)
        {
            minimizeToTrayCheckBox.IsChecked = _viewModel.MinimizeToTray;
        }

        // 更新其他设置复选框
        var checkUpdatesCheckBox = this.FindControl<CheckBox>("CheckUpdatesCheckBox");
        if (checkUpdatesCheckBox != null)
        {
            checkUpdatesCheckBox.IsChecked = _viewModel.CheckUpdates;
        }

        var autoSaveCheckBox = this.FindControl<CheckBox>("AutoSaveCheckBox");
        if (autoSaveCheckBox != null)
        {
            autoSaveCheckBox.IsChecked = _viewModel.AutoSave;
        }

        // 更新文件处理设置
        var defaultSavePathTextBox = this.FindControl<TextBox>("DefaultSavePathTextBox");
        if (defaultSavePathTextBox != null)
        {
            defaultSavePathTextBox.Text = _viewModel.DefaultSavePath;
        }

        var fileNamingRuleComboBox = this.FindControl<ComboBox>("FileNamingRuleComboBox");
        if (fileNamingRuleComboBox != null)
        {
            fileNamingRuleComboBox.SelectedIndex = GetNamingRuleIndex(_viewModel.FileNamingRule);
        }

        var createBackupCheckBox = this.FindControl<CheckBox>("CreateBackupCheckBox");
        if (createBackupCheckBox != null)
        {
            createBackupCheckBox.IsChecked = _viewModel.CreateBackup;
        }

        var backupFileCountNumeric = this.FindControl<NumericUpDown>("BackupFileCountNumeric");
        if (backupFileCountNumeric != null)
        {
            backupFileCountNumeric.Value = _viewModel.BackupFileCount;
        }

        // 更新高级设置
        var enableLoggingCheckBox = this.FindControl<CheckBox>("EnableLoggingCheckBox");
        if (enableLoggingCheckBox != null)
        {
            enableLoggingCheckBox.IsChecked = _viewModel.EnableLogging;
        }

        var logLevelComboBox = this.FindControl<ComboBox>("LogLevelComboBox");
        if (logLevelComboBox != null)
        {
            logLevelComboBox.SelectedIndex = GetLogLevelIndex(_viewModel.LogLevel);
        }

        var enableDebugModeCheckBox = this.FindControl<CheckBox>("EnableDebugModeCheckBox");
        if (enableDebugModeCheckBox != null)
        {
            enableDebugModeCheckBox.IsChecked = _viewModel.EnableDebugMode;
        }

        var enableExperimentalFeaturesCheckBox = this.FindControl<CheckBox>("EnableExperimentalFeaturesCheckBox");
        if (enableExperimentalFeaturesCheckBox != null)
        {
            enableExperimentalFeaturesCheckBox.IsChecked = _viewModel.EnableExperimentalFeatures;
        }
    }

    /// <summary>
    /// 获取主题索引
    /// 根据主题字符串获取对应的索引
    /// </summary>
    /// <param name="theme">主题字符串</param>
    /// <returns>对应的主题索引</returns>
    private int GetThemeIndex(string? theme)
    {
        if (string.IsNullOrEmpty(theme)) return 0;
        
        for (int i = 0; i < SystemSettingsConfig.ThemeOptions.Count; i++)
        {
            if (SystemSettingsConfig.ThemeOptions[i].DisplayName == theme)
                return i;
        }
        return 0;
    }

    /// <summary>
    /// 获取语言索引
    /// 根据语言字符串获取对应的索引
    /// </summary>
    /// <param name="language">语言字符串</param>
    /// <returns>对应的语言索引</returns>
    private int GetLanguageIndex(string? language)
    {
        if (string.IsNullOrEmpty(language)) return 0;
        
        for (int i = 0; i < SystemSettingsConfig.LanguageOptions.Count; i++)
        {
            if (SystemSettingsConfig.LanguageOptions[i].DisplayName == language)
                return i;
        }
        return 0;
    }

    /// <summary>
    /// 获取命名规则索引
    /// 根据命名规则字符串获取对应的索引
    /// </summary>
    /// <param name="rule">命名规则字符串</param>
    /// <returns>对应的命名规则索引</returns>
    private int GetNamingRuleIndex(string? rule)
    {
        if (string.IsNullOrEmpty(rule)) return 0;
        
        for (int i = 0; i < SystemSettingsConfig.FileNamingRuleOptions.Count; i++)
        {
            if (SystemSettingsConfig.FileNamingRuleOptions[i].DisplayName == rule)
                return i;
        }
        return 0;
    }

    /// <summary>
    /// 获取日志级别索引
    /// 根据日志级别字符串获取对应的索引
    /// </summary>
    /// <param name="level">日志级别字符串</param>
    /// <returns>对应的日志级别索引</returns>
    private int GetLogLevelIndex(string? level)
    {
        if (string.IsNullOrEmpty(level)) return 0;
        
        for (int i = 0; i < SystemSettingsConfig.LogLevelOptions.Count; i++)
        {
            if (SystemSettingsConfig.LogLevelOptions[i].DisplayName == level)
                return i;
        }
        return 0;
    }
}
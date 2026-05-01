using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Diagnostics;
using Avalonia.Markup.Xaml;
using SmartToolbox.ViewModels;
using SmartToolbox.Views;

namespace SmartToolbox;

/// <summary>
/// 应用程序主类
/// 负责应用程序的初始化和生命周期管理
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 初始化应用程序
    /// 加载Avalonia XAML资源
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成时调用
    /// 设置主窗口和数据上下文
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                Debug.WriteLine("正在创建主窗口...");
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                desktop.MainWindow = mainWindow;

                Debug.WriteLine("主窗口创建完成");

                // 确保窗口显示
                mainWindow.Show();
                mainWindow.Activate();
                Debug.WriteLine("主窗口已显示");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"框架初始化失败: {ex.Message}");
            Debug.WriteLine($"详细信息: {ex}");
            throw;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 禁用Avalonia数据注解验证
    /// 避免Avalonia和CommunityToolkit的重复验证
    /// </summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:Il2cpp", Justification = "Avalonia数据验证插件在运行时可用")]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        try
        {
            // 获取需要移除的验证插件数组
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // 移除找到的每个条目
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
        catch
        {
            // 如果禁用验证失败，忽略错误继续运行
            // 这在某些配置下可能是正常的
        }
    }
}
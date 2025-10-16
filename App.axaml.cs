using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 禁用Avalonia数据注解验证
    /// 避免Avalonia和CommunityToolkit的重复验证
    /// </summary>
    private void DisableAvaloniaDataAnnotationValidation()
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
}
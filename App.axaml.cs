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
using SmartToolbox.Services;

namespace SmartToolbox;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            InitializeServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();

                Debug.WriteLine("正在创建主窗口...");
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                desktop.MainWindow = mainWindow;

                Debug.WriteLine("主窗口创建完成");

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

    private void InitializeServices()
    {
        try
        {
            Debug.WriteLine("正在初始化服务...");

            var config = AIConfigManager.LoadConfig();
            AIService.Instance.Configure(config);

            ServiceLocator.Instance.Initialize();

            Debug.WriteLine("服务初始化完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"服务初始化失败: {ex.Message}");
        }
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026:Il2cpp", Justification = "Avalonia数据验证插件在运行时可用")]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        try
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
        catch
        {
        }
    }
}

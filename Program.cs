using Avalonia;
using System;

namespace SmartToolbox;

/// <summary>
/// 程序入口类
/// 包含应用程序的主入口点和Avalonia配置
/// </summary>
sealed class Program
{
    /// <summary>
    /// 应用程序主入口点
    /// 初始化代码。在调用AppMain之前不要使用任何Avalonia、第三方API或任何依赖SynchronizationContext的代码：
    /// 此时尚未初始化，可能会导致问题。
    /// </summary>
    /// <param name="args">命令行参数</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Avalonia配置方法，不要移除；可视化设计器也会使用
    /// 配置Avalonia应用程序构建器
    /// </summary>
    /// <returns>配置好的AppBuilder实例</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

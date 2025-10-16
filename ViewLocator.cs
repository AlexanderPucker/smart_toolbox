using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SmartToolbox.ViewModels;

namespace SmartToolbox;

/// <summary>
/// 视图定位器类
/// 实现IDataTemplate接口，用于根据视图模型自动定位对应的视图
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// 根据视图模型创建对应的视图控件
    /// 通过将视图模型类型名称中的"ViewModel"替换为"View"来查找对应的视图类型
    /// </summary>
    /// <param name="param">视图模型实例</param>
    /// <returns>对应的视图控件，如果未找到则返回显示错误信息的TextBlock</returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        // 将视图模型类型名称中的"ViewModel"替换为"View"来构造视图类型名称
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            // 创建并返回视图实例
            return (Control)Activator.CreateInstance(type)!;
        }
        
        // 如果未找到对应的视图类型，返回显示错误信息的TextBlock
        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// 检查数据是否与该模板匹配
    /// </summary>
    /// <param name="data">要检查的数据</param>
    /// <returns>如果数据是ViewModelBase类型则返回true，否则返回false</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}

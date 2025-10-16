using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SmartToolbox.ViewModels;

/// <summary>
/// 主窗口视图模型类
/// 负责管理主窗口的工具选择和内容显示
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 窗口标题
    /// </summary>
    [ObservableProperty]
    private string _title = "个人工具箱";

    /// <summary>
    /// 当前选中工具的名称
    /// </summary>
    [ObservableProperty]
    private string _selectedToolName = "欢迎使用";

    /// <summary>
    /// 当前显示的内容视图
    /// </summary>
    [ObservableProperty]
    private object? _currentContent;

    /// <summary>
    /// 可用工具集合
    /// </summary>
    public ObservableCollection<ToolItem> Tools { get; } = new();

    /// <summary>
    /// 构造函数
    /// 初始化工具列表并默认选择第一个工具
    /// </summary>
    public MainWindowViewModel()
    {
        InitializeTools();
        // 默认选择第一个工具
        if (Tools.Count > 0)
        {
            SelectTool(Tools[0]);
        }
    }

    /// <summary>
    /// 初始化工具列表
    /// 添加预定义的工具项到工具集合中
    /// </summary>
    private void InitializeTools()
    {
        Tools.Add(new ToolItem("📁", "文件移动工具", "文件移动、批量操作等"));
        Tools.Add(new ToolItem("⚙️", "系统设置", "系统设置配置"));
    }

    /// <summary>
    /// 选择工具命令
    /// 当用户在UI中选择一个工具时执行
    /// </summary>
    /// <param name="tool">选中的工具项</param>
    [RelayCommand]
    private void SelectTool(ToolItem tool)
    {
        SelectedToolName = tool.Name;
        
        // 这里可以根据工具类型创建对应的内容视图
        CurrentContent = CreateToolContent(tool.Name);
    }

    /// <summary>
    /// 创建工具内容视图
    /// 根据工具名称返回对应的视图实例
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <returns>工具对应的内容视图</returns>
    private object CreateToolContent(string toolName)
    {
        return toolName switch
        {
            "文件移动工具" => new SmartToolbox.Views.FileMoverView(),
            "系统设置" => new SmartToolbox.Views.SystemSettingsView() 
            { 
                DataContext = new SmartToolbox.ViewModels.SystemSettingsViewModel() 
            },
            _ => "欢迎使用个人工具箱！\n\n请从左侧选择需要使用的工具。\n\n这个工具箱包含了日常开发和办公中常用的实用工具。"
        };
    }
}

/// <summary>
/// 工具项类
/// 表示一个可用的工具，包含图标、名称和描述
/// </summary>
public class ToolItem
{
    /// <summary>
    /// 工具图标（Unicode字符或Emoji）
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="icon">工具图标</param>
    /// <param name="name">工具名称</param>
    /// <param name="description">工具描述</param>
    public ToolItem(string icon, string name, string description)
    {
        Icon = icon;
        Name = name;
        Description = description;
    }
}

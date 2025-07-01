using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SmartToolbox.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "个人工具箱";

    [ObservableProperty]
    private string _selectedToolName = "欢迎使用";

    [ObservableProperty]
    private object? _currentContent;

    public ObservableCollection<ToolItem> Tools { get; } = new();

    public MainWindowViewModel()
    {
        InitializeTools();
        // 默认选择第一个工具
        if (Tools.Count > 0)
        {
            SelectTool(Tools[0]);
        }
    }

    private void InitializeTools()
    {
        Tools.Add(new ToolItem("📁", "文件移动工具", "文件移动、批量操作等"));
        Tools.Add(new ToolItem("🔢", "计算器", "数学计算、单位转换"));
        Tools.Add(new ToolItem("🎨", "颜色工具", "颜色选择、格式转换"));
        Tools.Add(new ToolItem("🌐", "网络工具", "URL编码、IP查询等"));
        Tools.Add(new ToolItem("📊", "数据工具", "JSON格式化、Base64编码"));
        Tools.Add(new ToolItem("⚙️", "系统信息", "系统信息查看"));
    }

    [RelayCommand]
    private void SelectTool(ToolItem tool)
    {
        SelectedToolName = tool.Name;
        
        // 这里可以根据工具类型创建对应的内容视图
        CurrentContent = CreateToolContent(tool.Name);
    }

    private object CreateToolContent(string toolName)
    {
        return toolName switch
        {
            "文件移动工具" => new SmartToolbox.Views.FileMoverView(),
            "计算器" => "这里是计算器的内容区域\n\n• 基础数学运算\n• 科学计算\n• 单位转换\n• 进制转换",
            "颜色工具" => "这里是颜色工具的内容区域\n\n• 颜色选择器\n• RGB/HEX转换\n• 调色板\n• 颜色对比度检查",
            "网络工具" => "这里是网络工具的内容区域\n\n• URL 编码/解码\n• IP 地址查询\n• 端口扫描\n• 网络状态检查",
            "数据工具" => "这里是数据工具的内容区域\n\n• JSON 格式化\n• Base64 编码/解码\n• MD5/SHA 哈希\n• 时间戳转换",
            "系统信息" => "这里是系统信息的内容区域\n\n• CPU 信息\n• 内存使用情况\n• 磁盘空间\n• 网络接口",
            _ => "欢迎使用个人工具箱！\n\n请从左侧选择需要使用的工具。\n\n这个工具箱包含了日常开发和办公中常用的实用工具。"
        };
    }
}

public class ToolItem
{
    public string Icon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public ToolItem(string icon, string name, string description)
    {
        Icon = icon;
        Name = name;
        Description = description;
    }
}

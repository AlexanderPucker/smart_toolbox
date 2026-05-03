using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SmartToolbox.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Smart Toolbox";

    [ObservableProperty]
    private string _selectedToolName = "欢迎使用";

    [ObservableProperty]
    private object? _currentContent;

    public ObservableCollection<ToolCategory> Categories { get; } = new();

    public MainWindowViewModel()
    {
        InitializeTools();
        if (Categories.Count > 0 && Categories[0].Tools.Count > 0)
        {
            SelectTool(Categories[0].Tools[0]);
        }
    }

    private void InitializeTools()
    {
        var aiCategory = new ToolCategory("🤖 AI 工具", new()
        {
            new ToolItem("💬", "AI 对话", "智能对话问答系统，支持流式输出"),
            new ToolItem("🌍", "AI 翻译", "多语言智能翻译"),
            new ToolItem("📝", "AI 摘要", "自动提取文本核心内容"),
            new ToolItem("✨", "AI 润色", "改善文本质量和表达"),
            new ToolItem("💻", "代码解释", "解释代码功能和逻辑"),
            new ToolItem("🔍", "正则生成", "用自然语言生成正则表达式"),
            new ToolItem("🎯", "Prompt优化", "分析和优化提示词"),
            new ToolItem("📚", "知识库", "RAG文档问答系统"),
            new ToolItem("⚡", "工作流", "AI工作流自动化"),
            new ToolItem("🔧", "AI 设置", "配置AI API和参数"),
        });

        var devCategory = new ToolCategory("🔧 开发工具", new()
        {
            new ToolItem("📋", "JSON格式化", "JSON格式化、压缩、校验"),
            new ToolItem("🔐", "哈希计算", "MD5、SHA1、SHA256、SHA512"),
            new ToolItem("🔄", "Base64", "Base64编码、解码"),
            new ToolItem("⏱️", "时间戳", "Unix时间戳与日期互转"),
            new ToolItem("🆔", "UUID", "批量生成UUID/GUID"),
            new ToolItem("🏃", "代码沙盒", "在线运行代码"),
        });

        var fileCategory = new ToolCategory("📁 文件工具", new()
        {
            new ToolItem("📂", "文件移动", "文件移动、批量操作"),
        });

        var statsCategory = new ToolCategory("📊 统计分析", new()
        {
            new ToolItem("💰", "用量统计", "Token使用量和费用统计"),
        });

        var promptCategory = new ToolCategory("📝 Prompt管理", new()
        {
            new ToolItem("📋", "Prompt模板", "管理和使用Prompt模板"),
        });

        Categories.Add(aiCategory);
        Categories.Add(devCategory);
        Categories.Add(fileCategory);
        Categories.Add(statsCategory);
        Categories.Add(promptCategory);
    }

    [RelayCommand]
    private void SelectTool(ToolItem tool)
    {
        SelectedToolName = tool.Name;
        CurrentContent = CreateToolContent(tool.Name);
    }

    private object CreateToolContent(string toolName)
    {
        return toolName switch
        {
            "AI 对话" => new SmartToolbox.Views.AIChatView(),
            "AI 翻译" => new SmartToolbox.Views.AITranslatorView(),
            "AI 摘要" => new SmartToolbox.Views.AISummaryView(),
            "AI 润色" => new SmartToolbox.Views.AITextPolishView(),
            "代码解释" => new SmartToolbox.Views.AICodeExplainView(),
            "正则生成" => new SmartToolbox.Views.AIRegexGeneratorView(),
            "Prompt优化" => new SmartToolbox.Views.PromptOptimizerView(),
            "知识库" => new SmartToolbox.Views.KnowledgeBaseView(),
            "工作流" => new SmartToolbox.Views.WorkflowView(),
            "AI 设置" => new SmartToolbox.Views.AISettingsView(),
            "JSON格式化" => new SmartToolbox.Views.JsonFormatterView(),
            "哈希计算" => new SmartToolbox.Views.HashCalculatorView(),
            "Base64" => new SmartToolbox.Views.Base64View(),
            "时间戳" => new SmartToolbox.Views.TimestampView(),
            "UUID" => new SmartToolbox.Views.UuidGeneratorView(),
            "代码沙盒" => new SmartToolbox.Views.CodeSandboxView(),
            "文件移动" => new SmartToolbox.Views.FileMoverView(),
            "用量统计" => new SmartToolbox.Views.UsageStatsView(),
            "Prompt模板" => new SmartToolbox.Views.PromptTemplateView(),
            _ => "欢迎使用 Smart Toolbox！\n\n请从左侧选择需要使用的工具。"
        };
    }
}

public class ToolCategory
{
    public string Name { get; set; }
    public ObservableCollection<ToolItem> Tools { get; set; }

    public ToolCategory(string name, ObservableCollection<ToolItem> tools)
    {
        Name = name;
        Tools = tools;
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

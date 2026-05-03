using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Models;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class PromptTemplateViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string _templateCategory = string.Empty;

    [ObservableProperty]
    private string _templateDescription = string.Empty;

    [ObservableProperty]
    private string _templateContent = string.Empty;

    [ObservableProperty]
    private PromptTemplate? _selectedTemplate;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "管理Prompt模板";

    [ObservableProperty]
    private string _outputText = string.Empty;

    public ObservableCollection<PromptTemplate> Templates { get; }

    public ObservableCollection<string> Categories { get; } = new()
    {
        "全部",
        "编程",
        "办公",
        "写作",
        "自定义"
    };

    [ObservableProperty]
    private string _selectedCategory = "全部";

    public PromptTemplateViewModel()
    {
        Templates = PromptTemplateManager.LoadTemplates();
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterTemplates();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        FilterTemplates();
    }

    private void FilterTemplates()
    {
    }

    [RelayCommand]
    private void NewTemplate()
    {
        TemplateName = string.Empty;
        TemplateCategory = "自定义";
        TemplateDescription = string.Empty;
        TemplateContent = string.Empty;
        SelectedTemplate = null;
        StatusMessage = "新建模板";
    }

    [RelayCommand]
    private void SaveTemplate()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            StatusMessage = "请输入模板名称";
            return;
        }

        if (string.IsNullOrWhiteSpace(TemplateContent))
        {
            StatusMessage = "请输入模板内容";
            return;
        }

        if (SelectedTemplate != null)
        {
            SelectedTemplate.Name = TemplateName;
            SelectedTemplate.Category = TemplateCategory;
            SelectedTemplate.Description = TemplateDescription;
            SelectedTemplate.Template = TemplateContent;
            PromptTemplateManager.UpdateTemplate(SelectedTemplate);
            StatusMessage = $"模板已更新: {TemplateName}";
        }
        else
        {
            var newTemplate = new PromptTemplate
            {
                Name = TemplateName,
                Category = TemplateCategory,
                Description = TemplateDescription,
                Template = TemplateContent
            };
            Templates.Add(newTemplate);
            PromptTemplateManager.AddTemplate(newTemplate);
            StatusMessage = $"模板已保存: {TemplateName}";
        }
    }

    [RelayCommand]
    private void EditTemplate()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "请先选择要编辑的模板";
            return;
        }

        TemplateName = SelectedTemplate.Name;
        TemplateCategory = SelectedTemplate.Category;
        TemplateDescription = SelectedTemplate.Description;
        TemplateContent = SelectedTemplate.Template;
        StatusMessage = $"编辑模板: {SelectedTemplate.Name}";
    }

    [RelayCommand]
    private void DeleteTemplate()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "请先选择要删除的模板";
            return;
        }

        var name = SelectedTemplate.Name;
        Templates.Remove(SelectedTemplate);
        PromptTemplateManager.DeleteTemplate(SelectedTemplate.Id);
        NewTemplate();
        StatusMessage = $"模板已删除: {name}";
    }

    [RelayCommand]
    private void LoadSelectedTemplate()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "请先选择一个模板";
            return;
        }

        TemplateName = SelectedTemplate.Name;
        TemplateCategory = SelectedTemplate.Category;
        TemplateDescription = SelectedTemplate.Description;
        TemplateContent = SelectedTemplate.Template;
        StatusMessage = $"已加载模板: {SelectedTemplate.Name}";
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task RunTemplateAsync()
    {
        if (SelectedTemplate == null)
        {
            StatusMessage = "请先选择一个模板";
            return;
        }

        var aiService = new AIService();
        var config = AIConfigManager.LoadConfig();
        aiService.Configure(config);

        var prompt = SelectedTemplate.Template;
        var variables = ExtractVariables(prompt);
        
        foreach (var variable in variables)
        {
            var value = $"[请输入{variable}]";
            prompt = prompt.Replace($"{{{variable}}}", value);
        }

        StatusMessage = "正在执行模板...";

        try
        {
            OutputText = await aiService.SendMessageAsync(prompt);
            StatusMessage = $"模板执行成功: {SelectedTemplate.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"执行失败: {ex.Message}";
        }
    }

    private List<string> ExtractVariables(string text)
    {
        var variables = new List<string>();
        var startIndex = 0;
        
        while ((startIndex = text.IndexOf('{', startIndex)) != -1)
        {
            var endIndex = text.IndexOf('}', startIndex);
            if (endIndex == -1) break;
            
            var variable = text.Substring(startIndex + 1, endIndex - startIndex - 1);
            if (!variables.Contains(variable))
            {
                variables.Add(variable);
            }
            startIndex = endIndex + 1;
        }
        
        return variables;
    }
}

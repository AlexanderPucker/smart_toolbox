using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public static class PromptTemplateManager
{
    private static readonly string TemplatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartToolbox",
        "prompt_templates.json");

    private static ObservableCollection<PromptTemplate>? _templates;

    public static ObservableCollection<PromptTemplate> LoadTemplates()
    {
        if (_templates != null)
            return _templates;

        if (!File.Exists(TemplatePath))
        {
            _templates = new ObservableCollection<PromptTemplate>(DefaultTemplates.GetDefaults());
            SaveTemplates(_templates);
            return _templates;
        }

        try
        {
            var json = File.ReadAllText(TemplatePath);
            var templates = JsonSerializer.Deserialize<ObservableCollection<PromptTemplate>>(json);
            _templates = templates ?? new ObservableCollection<PromptTemplate>(DefaultTemplates.GetDefaults());
            return _templates;
        }
        catch
        {
            _templates = new ObservableCollection<PromptTemplate>(DefaultTemplates.GetDefaults());
            return _templates;
        }
    }

    public static void SaveTemplates(ObservableCollection<PromptTemplate> templates)
    {
        try
        {
            var directory = Path.GetDirectoryName(TemplatePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(templates, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(TemplatePath, json);
            _templates = templates;
        }
        catch
        {
        }
    }

    public static void AddTemplate(PromptTemplate template)
    {
        var templates = LoadTemplates();
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.Now;
        template.UpdatedAt = DateTime.Now;
        templates.Add(template);
        SaveTemplates(templates);
    }

    public static void UpdateTemplate(PromptTemplate template)
    {
        var templates = LoadTemplates();
        var existing = templates.FirstOrDefault(t => t.Id == template.Id);
        if (existing != null)
        {
            existing.Name = template.Name;
            existing.Category = template.Category;
            existing.Description = template.Description;
            existing.Template = template.Template;
            existing.UpdatedAt = DateTime.Now;
            SaveTemplates(templates);
        }
    }

    public static void DeleteTemplate(Guid id)
    {
        var templates = LoadTemplates();
        var template = templates.FirstOrDefault(t => t.Id == id);
        if (template != null)
        {
            templates.Remove(template);
            SaveTemplates(templates);
        }
    }

    public static string ReplaceVariables(string template, params (string variable, string value)[] variables)
    {
        var result = template;
        foreach (var (variable, value) in variables)
        {
            result = result.Replace($"{{{variable}}}", value);
        }
        return result;
    }
}

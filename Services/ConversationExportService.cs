using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public enum ExportFormat
{
    Json,
    Markdown,
    Html,
    PlainText,
    Pdf
}

public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Markdown;
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeTimestamps { get; set; } = true;
    public bool IncludeTokenCounts { get; set; }
    public bool IncludeCosts { get; set; }
    public bool Anonymize { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
}

public sealed class ConversationExportService
{
    private static readonly Lazy<ConversationExportService> _instance = new(() => new ConversationExportService());
    public static ConversationExportService Instance => _instance.Value;

    private readonly ConversationManager _conversationManager;

    public event Action<string, string>? OnExportCompleted;

    private ConversationExportService()
    {
        _conversationManager = ConversationManager.Instance;
    }

    public async Task ExportConversationAsync(
        Guid conversationId,
        string outputPath,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        var conversation = _conversationManager.GetConversation(conversationId.ToString());
        if (conversation == null)
            throw new ArgumentException($"未找到对话: {conversationId}");

        var content = await GenerateExportContentAsync(conversation, options);

        await File.WriteAllTextAsync(outputPath, content);
        OnExportCompleted?.Invoke(conversationId.ToString(), outputPath);
    }

    public async Task ExportAllConversationsAsync(
        string outputDirectory,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        var conversations = _conversationManager.GetAllConversations();

        foreach (var conversation in conversations)
        {
            var fileName = SanitizeFileName(conversation.Title) + GetFileExtension(options.Format);
            var outputPath = Path.Combine(outputDirectory, fileName);

            try
            {
                await ExportConversationAsync(conversation.Id, outputPath, options);
            }
            catch { }
        }
    }

    public async Task ExportToArchiveAsync(
        List<Guid> conversationIds,
        string archivePath,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        using var archiveStream = new FileStream(archivePath, FileMode.Create);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create);

        foreach (var conversationId in conversationIds)
        {
            var conversation = _conversationManager.GetConversation(conversationId.ToString());
            if (conversation == null) continue;

            var content = await GenerateExportContentAsync(conversation, options);
            var fileName = SanitizeFileName(conversation.Title) + GetFileExtension(options.Format);

            var entry = archive.CreateEntry(fileName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync(content);
        }

        OnExportCompleted?.Invoke("archive", archivePath);
    }

    private async Task<string> GenerateExportContentAsync(Conversation conversation, ExportOptions options)
    {
        return options.Format switch
        {
            ExportFormat.Json => await ExportAsJsonAsync(conversation, options),
            ExportFormat.Markdown => ExportAsMarkdown(conversation, options),
            ExportFormat.Html => ExportAsHtml(conversation, options),
            ExportFormat.PlainText => ExportAsPlainText(conversation, options),
            _ => ExportAsMarkdown(conversation, options)
        };
    }

    private Task<string> ExportAsJsonAsync(Conversation conversation, ExportOptions options)
    {
        var exportData = new
        {
            Title = options.Anonymize ? "对话记录" : conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            MessageCount = conversation.Messages.Count,
            Messages = conversation.Messages.Select(m => new
            {
                Role = m.Role,
                Content = options.Anonymize ? AnonymizeContent(m.Content) : m.Content,
                Timestamp = options.IncludeTimestamps ? m.Timestamp : (DateTime?)null,
                TokenCount = options.IncludeTokenCounts ? m.TokenCount : (int?)null
            }),
            Metadata = options.IncludeMetadata ? new
            {
                conversation.TotalTokens,
                conversation.TotalCost,
                conversation.Model,
                Tags = conversation.Tags
            } : null
        };

        return Task.FromResult(JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private string ExportAsMarkdown(Conversation conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {EscapeMarkdown(options.Anonymize ? "对话记录" : conversation.Title)}");
        sb.AppendLine();

        if (options.IncludeMetadata)
        {
            sb.AppendLine("## 信息");
            sb.AppendLine();
            sb.AppendLine($"- 创建时间: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- 消息数: {conversation.Messages.Count}");

            if (options.IncludeTokenCounts)
            {
                sb.AppendLine($"- 总Token: {conversation.TotalTokens}");
            }

            if (options.IncludeCosts)
            {
                sb.AppendLine($"- 总费用: ${conversation.TotalCost:F4}");
            }

            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(conversation.SystemPrompt))
        {
            sb.AppendLine("## 系统提示词");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(conversation.SystemPrompt);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("## 对话内容");
        sb.AppendLine();

        foreach (var message in conversation.Messages)
        {
            var role = message.Role switch
            {
                "user" => "👤 用户",
                "assistant" => "🤖 助手",
                "system" => "⚙️ 系统",
                _ => message.Role
            };

            sb.AppendLine($"### {role}");

            if (options.IncludeTimestamps)
            {
                sb.AppendLine($"*{message.Timestamp:yyyy-MM-dd HH:mm:ss}*");
                sb.AppendLine();
            }

            var content = options.Anonymize ? AnonymizeContent(message.Content) : message.Content;
            sb.AppendLine(content);
            sb.AppendLine();

            if (options.IncludeTokenCounts && message.TokenCount > 0)
            {
                sb.AppendLine($"*Token: {message.TokenCount}*");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine($"*导出于 {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");

        return sb.ToString();
    }

    private string ExportAsHtml(Conversation conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>{EscapeHtml(options.Anonymize ? "对话记录" : conversation.Title)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; line-height: 1.6; }");
        sb.AppendLine(".message { margin: 20px 0; padding: 15px; border-radius: 8px; }");
        sb.AppendLine(".user { background: #e3f2fd; }");
        sb.AppendLine(".assistant { background: #f5f5f5; }");
        sb.AppendLine(".system { background: #fff3e0; }");
        sb.AppendLine(".role { font-weight: bold; margin-bottom: 10px; }");
        sb.AppendLine(".timestamp { color: #666; font-size: 0.85em; }");
        sb.AppendLine(".content { white-space: pre-wrap; }");
        sb.AppendLine("pre { background: #f0f0f0; padding: 10px; border-radius: 4px; overflow-x: auto; }");
        sb.AppendLine("code { background: #f0f0f0; padding: 2px 6px; border-radius: 3px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>{EscapeHtml(options.Anonymize ? "对话记录" : conversation.Title)}</h1>");

        if (options.IncludeMetadata)
        {
            sb.AppendLine("<div class=\"metadata\">");
            sb.AppendLine($"<p>创建时间: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine($"<p>消息数: {conversation.Messages.Count}</p>");
            if (options.IncludeTokenCounts)
            {
                sb.AppendLine($"<p>总Token: {conversation.TotalTokens}</p>");
            }
            sb.AppendLine("</div>");
        }

        foreach (var message in conversation.Messages)
        {
            sb.AppendLine($"<div class=\"message {message.Role}\">");
            sb.AppendLine($"<div class=\"role\">{EscapeHtml(message.Role)}</div>");

            if (options.IncludeTimestamps)
            {
                sb.AppendLine($"<div class=\"timestamp\">{message.Timestamp:yyyy-MM-dd HH:mm:ss}</div>");
            }

            var content = options.Anonymize ? AnonymizeContent(message.Content) : message.Content;
            sb.AppendLine($"<div class=\"content\">{EscapeHtml(content)}</div>");

            sb.AppendLine("</div>");
        }

        sb.AppendLine($"<hr><p><em>导出于 {DateTime.Now:yyyy-MM-dd HH:mm:ss}</em></p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string ExportAsPlainText(Conversation conversation, ExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"=== {options.Anonymize ? "对话记录" : conversation.Title} ===");
        sb.AppendLine();

        if (options.IncludeMetadata)
        {
            sb.AppendLine($"创建时间: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"消息数: {conversation.Messages.Count}");
            sb.AppendLine();
        }

        foreach (var message in conversation.Messages)
        {
            sb.AppendLine($"[{message.Role}]");

            if (options.IncludeTimestamps)
            {
                sb.AppendLine($"时间: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }

            var content = options.Anonymize ? AnonymizeContent(message.Content) : message.Content;
            sb.AppendLine(content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string AnonymizeContent(string content)
    {
        var result = content;

        var emailPattern = @"[\w\.-]+@[\w\.-]+\.\w+";
        result = System.Text.RegularExpressions.Regex.Replace(result, emailPattern, "[邮箱]");

        var phonePattern = @"\d{11}";
        result = System.Text.RegularExpressions.Regex.Replace(result, phonePattern, "[电话]");

        return result;
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var result = new StringBuilder();

        foreach (var c in fileName)
        {
            if (!invalidChars.Contains(c))
            {
                result.Append(c);
            }
        }

        return string.IsNullOrWhiteSpace(result.ToString()) ? "untitled" : result.ToString().Trim();
    }

    private string GetFileExtension(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Json => ".json",
            ExportFormat.Markdown => ".md",
            ExportFormat.Html => ".html",
            ExportFormat.PlainText => ".txt",
            ExportFormat.Pdf => ".pdf",
            _ => ".txt"
        };
    }

    private string EscapeMarkdown(string text)
    {
        return text.Replace("*", "\\*").Replace("_", "\\_").Replace("#", "\\#");
    }

    private string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    public async Task ImportConversationAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        if (extension == ".json")
        {
            var json = await File.ReadAllTextAsync(filePath);
            var conversation = JsonSerializer.Deserialize<Conversation>(json);

            if (conversation != null)
            {
                conversation.Id = Guid.NewGuid();
                conversation.CreatedAt = DateTime.Now;
                conversation.UpdatedAt = DateTime.Now;

                ConversationManager.Instance.CreateConversation(
                    conversation.Title,
                    conversation.SystemPrompt
                );
            }
        }
    }

    public string GenerateShareableLink(Conversation conversation, ExportFormat format = ExportFormat.Markdown)
    {
        var content = ExportAsMarkdown(conversation, new ExportOptions());
        var bytes = Encoding.UTF8.GetBytes(content);
        var base64 = Convert.ToBase64String(bytes);

        return $"data:text/markdown;base64,{base64}";
    }
}

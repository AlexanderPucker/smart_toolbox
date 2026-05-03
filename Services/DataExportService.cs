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
    Pdf,
    Word
}

public enum ExportScope
{
    CurrentConversation,
    SelectedConversations,
    AllConversations,
    DateRange
}

public class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Markdown;
    public ExportScope Scope { get; set; } = ExportScope.CurrentConversation;
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeTimestamps { get; set; } = true;
    public bool IncludeTokenCounts { get; set; }
    public bool IncludeCosts { get; set; }
    public bool Anonymize { get; set; }
    public bool IncludeSystemPrompts { get; set; } = true;
    public bool IncludePinnedOnly { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? Tags { get; set; }
    public bool CompressOutput { get; set; }
    public bool IncludeAttachments { get; set; }
}

public class ExportResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public int ConversationCount { get; set; }
    public int MessageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
}

public sealed class DataExportService
{
    private static readonly Lazy<DataExportService> _instance = new(() => new DataExportService());
    public static DataExportService Instance => _instance.Value;

    private readonly ConversationManager _conversationManager;
    private readonly string _exportDirectory;

    public event Action<ExportResult>? OnExportCompleted;
    public event Action<int>? OnExportProgress;

    private DataExportService()
    {
        _conversationManager = ConversationManager.Instance;
        _exportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "exports");
        Directory.CreateDirectory(_exportDirectory);
    }

    public async Task<ExportResult> ExportAsync(ExportOptions options, string? outputPath = null)
    {
        var startTime = DateTime.Now;
        var result = new ExportResult();

        try
        {
            var conversations = GetConversationsToExport(options);
            result.ConversationCount = conversations.Count;
            result.MessageCount = conversations.Sum(c => c.Messages.Count);

            outputPath ??= GenerateOutputPath(options.Format);
            result.OutputPath = outputPath;

            var content = await GenerateContentAsync(conversations, options, result);

            if (options.CompressOutput)
            {
                await CreateZipArchiveAsync(content, outputPath, options);
            }
            else
            {
                await File.WriteAllTextAsync(outputPath, content);
            }

            result.FileSizeBytes = new FileInfo(outputPath).Length;
            result.Success = true;
            result.Duration = DateTime.Now - startTime;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        OnExportCompleted?.Invoke(result);
        return result;
    }

    private List<Conversation> GetConversationsToExport(ExportOptions options)
    {
        var allConversations = _conversationManager.GetAllConversations();

        return options.Scope switch
        {
            ExportScope.CurrentConversation => allConversations.Take(1).ToList(),
            ExportScope.AllConversations => allConversations,
            ExportScope.DateRange => allConversations
                .Where(c => c.CreatedAt >= (options.StartDate ?? DateTime.MinValue) &&
                           c.CreatedAt <= (options.EndDate ?? DateTime.MaxValue))
                .ToList(),
            _ => allConversations
        };
    }

    private async Task<string> GenerateContentAsync(
        List<Conversation> conversations,
        ExportOptions options,
        ExportResult result)
    {
        return options.Format switch
        {
            ExportFormat.Json => await GenerateJsonAsync(conversations, options),
            ExportFormat.Markdown => await GenerateMarkdownAsync(conversations, options, result),
            ExportFormat.Html => await GenerateHtmlAsync(conversations, options),
            ExportFormat.PlainText => await GeneratePlainTextAsync(conversations, options),
            _ => await GenerateMarkdownAsync(conversations, options, result)
        };
    }

    private Task<string> GenerateJsonAsync(List<Conversation> conversations, ExportOptions options)
    {
        var exportData = new
        {
            ExportedAt = DateTime.Now,
            TotalConversations = conversations.Count,
            Conversations = conversations.Select(c => new
            {
                Id = c.Id,
                Title = options.Anonymize ? "Conversation" : c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                SystemPrompt = options.IncludeSystemPrompts ? c.SystemPrompt : null,
                Messages = c.Messages
                    .Where(m => !options.IncludePinnedOnly || m.IsPinned)
                    .Select(m => new
                    {
                        Role = m.Role,
                        Content = options.Anonymize ? AnonymizeContent(m.Content) : m.Content,
                        Timestamp = options.IncludeTimestamps ? m.Timestamp : (DateTime?)null,
                        TokenCount = options.IncludeTokenCounts ? m.TokenCount : (int?)null,
                        IsPinned = m.IsPinned
                    }),
                Metadata = options.IncludeMetadata ? new
                {
                    c.TotalTokens,
                    c.TotalCost,
                    c.Model,
                    Tags = c.Tags
                } : null
            })
        };

        return Task.FromResult(JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private Task<string> GenerateMarkdownAsync(
        List<Conversation> conversations,
        ExportOptions options,
        ExportResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Smart Toolbox Export");
        sb.AppendLine();
        sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"对话数量: {conversations.Count}");
        sb.AppendLine();

        int processed = 0;
        foreach (var conversation in conversations)
        {
            processed++;
            OnExportProgress?.Invoke((int)((double)processed / conversations.Count * 100));

            sb.AppendLine($"## {EscapeMarkdown(options.Anonymize ? "对话" : conversation.Title)}");
            sb.AppendLine();

            if (options.IncludeMetadata)
            {
                sb.AppendLine("### 信息");
                sb.AppendLine();
                sb.AppendLine($"- 创建时间: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"- 更新时间: {conversation.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"- 消息数: {conversation.Messages.Count}");

                if (options.IncludeTokenCounts)
                {
                    sb.AppendLine($"- 总Token: {conversation.TotalTokens}");
                }

                if (options.IncludeCosts)
                {
                    sb.AppendLine($"- 总费用: ${conversation.TotalCost:F4}");
                }

                if (conversation.Tags.Count > 0)
                {
                    sb.AppendLine($"- 标签: {string.Join(", ", conversation.Tags)}");
                }

                sb.AppendLine();
            }

            if (options.IncludeSystemPrompts && !string.IsNullOrEmpty(conversation.SystemPrompt))
            {
                sb.AppendLine("### 系统提示词");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(conversation.SystemPrompt);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            sb.AppendLine("### 对话内容");
            sb.AppendLine();

            var messages = options.IncludePinnedOnly
                ? conversation.Messages.Where(m => m.IsPinned).ToList()
                : conversation.Messages;

            foreach (var message in messages)
            {
                var role = message.Role switch
                {
                    "user" => "👤 用户",
                    "assistant" => "🤖 助手",
                    "system" => "⚙️ 系统",
                    _ => message.Role
                };

                sb.AppendLine($"#### {role}");

                if (options.IncludeTimestamps)
                {
                    sb.AppendLine($"*{message.Timestamp:yyyy-MM-dd HH:mm:ss}*");
                    sb.AppendLine();
                }

                var content = options.Anonymize ? AnonymizeContent(message.Content) : message.Content;
                sb.AppendLine(content);
                sb.AppendLine();

                if (message.IsPinned)
                {
                    sb.AppendLine("*📌 已置顶*");
                    sb.AppendLine();
                }

                if (options.IncludeTokenCounts && message.TokenCount > 0)
                {
                    sb.AppendLine($"*Token: {message.TokenCount}*");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine($"*由 Smart Toolbox 导出*");

        return Task.FromResult(sb.ToString());
    }

    private Task<string> GenerateHtmlAsync(List<Conversation> conversations, ExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<title>Smart Toolbox Export</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GenerateHtmlStyles());
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("<h1>Smart Toolbox Export</h1>");
        sb.AppendLine($"<p>导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"<p>对话数量: {conversations.Count}</p>");

        foreach (var conversation in conversations)
        {
            sb.AppendLine("<div class=\"conversation\">");
            sb.AppendLine($"<h2>{EscapeHtml(options.Anonymize ? "对话" : conversation.Title)}</h2>");

            if (options.IncludeMetadata)
            {
                sb.AppendLine("<div class=\"metadata\">");
                sb.AppendLine($"<span>创建: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}</span>");
                sb.AppendLine($"<span>消息: {conversation.Messages.Count}</span>");
                if (options.IncludeCosts)
                {
                    sb.AppendLine($"<span>费用: ${conversation.TotalCost:F4}</span>");
                }
                sb.AppendLine("</div>");
            }

            var messages = options.IncludePinnedOnly
                ? conversation.Messages.Where(m => m.IsPinned).ToList()
                : conversation.Messages;

            foreach (var message in messages)
            {
                sb.AppendLine($"<div class=\"message {message.Role}\">");
                sb.AppendLine($"<div class=\"role\">{message.Role}</div>");

                if (options.IncludeTimestamps)
                {
                    sb.AppendLine($"<div class=\"timestamp\">{message.Timestamp:yyyy-MM-dd HH:mm:ss}</div>");
                }

                var content = options.Anonymize ? AnonymizeContent(message.Content) : message.Content;
                sb.AppendLine($"<div class=\"content\">{EscapeHtml(content)}</div>");

                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return Task.FromResult(sb.ToString());
    }

    private Task<string> GeneratePlainTextAsync(List<Conversation> conversations, ExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Smart Toolbox Export ===");
        sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"对话数量: {conversations.Count}");
        sb.AppendLine();

        foreach (var conversation in conversations)
        {
            sb.AppendLine($"=== {options.Anonymize ? "对话" : conversation.Title} ===");
            sb.AppendLine();

            if (options.IncludeMetadata)
            {
                sb.AppendLine($"创建时间: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"消息数: {conversation.Messages.Count}");
                sb.AppendLine();
            }

            var messages = options.IncludePinnedOnly
                ? conversation.Messages.Where(m => m.IsPinned).ToList()
                : conversation.Messages;

            foreach (var message in messages)
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

            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }

    private string GenerateHtmlStyles()
    {
        return @"
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #1a1a1a; color: #e0e0e0; margin: 0; padding: 20px; }
            .container { max-width: 900px; margin: 0 auto; }
            h1 { color: #fff; border-bottom: 2px solid #4a9eff; padding-bottom: 10px; }
            h2 { color: #4a9eff; margin-top: 30px; }
            .conversation { background: #2d2d2d; border-radius: 8px; padding: 20px; margin: 20px 0; }
            .metadata { color: #888; font-size: 0.9em; margin-bottom: 15px; }
            .metadata span { margin-right: 15px; }
            .message { margin: 15px 0; padding: 15px; border-radius: 8px; }
            .user { background: #1e3a5f; }
            .assistant { background: #2d2d2d; border: 1px solid #444; }
            .system { background: #3d2d1e; }
            .role { font-weight: bold; margin-bottom: 10px; color: #4a9eff; }
            .timestamp { color: #666; font-size: 0.85em; margin-bottom: 10px; }
            .content { white-space: pre-wrap; line-height: 1.6; }
        ";
    }

    private string AnonymizeContent(string content)
    {
        var result = content;

        var emailPattern = @"[\w\.-]+@[\w\.-]+\.\w+";
        result = System.Text.RegularExpressions.Regex.Replace(result, emailPattern, "[email]");

        var phonePattern = @"\d{11}";
        result = System.Text.RegularExpressions.Regex.Replace(result, phonePattern, "[phone]");

        return result;
    }

    private string GenerateOutputPath(ExportFormat format)
    {
        var extension = format switch
        {
            ExportFormat.Json => ".json",
            ExportFormat.Markdown => ".md",
            ExportFormat.Html => ".html",
            ExportFormat.PlainText => ".txt",
            _ => ".txt"
        };

        var fileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
        return Path.Combine(_exportDirectory, fileName);
    }

    private async Task CreateZipArchiveAsync(string content, string outputPath, ExportOptions options)
    {
        var zipPath = outputPath.Replace(Path.GetExtension(outputPath), ".zip");

        using var archiveStream = new FileStream(zipPath, FileMode.Create);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create);

        var entry = archive.CreateEntry(Path.GetFileName(outputPath));
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream);
        await writer.WriteAsync(content);
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

    public async Task<ExportResult> ExportAllDataAsync(string outputPath)
    {
        var options = new ExportOptions
        {
            Format = ExportFormat.Json,
            Scope = ExportScope.AllConversations,
            IncludeMetadata = true,
            IncludeTimestamps = true,
            IncludeTokenCounts = true,
            IncludeCosts = true,
            IncludeSystemPrompts = true
        };

        return await ExportAsync(options, outputPath);
    }

    public async Task ImportDataAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var importData = JsonSerializer.Deserialize<ImportData>(json);

            if (importData?.Conversations != null)
            {
                foreach (var conv in importData.Conversations)
                {
                    var conversation = _conversationManager.CreateConversation(
                        conv.Title ?? "Imported",
                        conv.SystemPrompt ?? ""
                    );

                    if (conv.Messages != null)
                    {
                        foreach (var msg in conv.Messages)
                        {
                            _conversationManager.AddMessage(conversation.Id, new Models.Message
                            {
                                Role = msg.Role ?? "user",
                                Content = msg.Content ?? "",
                                Timestamp = msg.Timestamp ?? DateTime.Now
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"导入失败: {ex.Message}");
        }
    }

    public void OpenExportDirectory()
    {
        if (Directory.Exists(_exportDirectory))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _exportDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    public List<string> GetRecentExports(int count = 10)
    {
        if (!Directory.Exists(_exportDirectory))
            return new List<string>();

        return Directory.GetFiles(_exportDirectory)
            .OrderByDescending(f => File.GetCreationTime(f))
            .Take(count)
            .ToList();
    }

    public void CleanOldExports(int keepDays = 30)
    {
        if (!Directory.Exists(_exportDirectory))
            return;

        var cutoff = DateTime.Now.AddDays(-keepDays);

        foreach (var file in Directory.GetFiles(_exportDirectory))
        {
            if (File.GetCreationTime(file) < cutoff)
            {
                try { File.Delete(file); } catch { }
            }
        }
    }
}

internal class ImportData
{
    public List<ImportedConversation>? Conversations { get; set; }
}

internal class ImportedConversation
{
    public string? Title { get; set; }
    public string? SystemPrompt { get; set; }
    public List<ImportedMessage>? Messages { get; set; }
}

internal class ImportedMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
    public DateTime? Timestamp { get; set; }
}

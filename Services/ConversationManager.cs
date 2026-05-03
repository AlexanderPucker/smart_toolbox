using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SmartToolbox.Models;

namespace SmartToolbox.Services;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "新对话";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<Message> Messages { get; set; } = new();
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
    public bool IsPinned { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Summary { get; set; }
}

public sealed class ConversationManager
{
    private static readonly Lazy<ConversationManager> _instance = new(() => new ConversationManager());
    public static ConversationManager Instance => _instance.Value;

    private readonly string _conversationsPath;
    private readonly string _conversationsIndexFile;
    private Dictionary<Guid, Conversation> _conversations = new();
    private Conversation? _currentConversation;

    public event Action<Conversation>? OnConversationCreated;
    public event Action<Conversation>? OnConversationUpdated;
    public event Action<Guid>? OnConversationDeleted;
    public event Action<Conversation>? OnCurrentConversationChanged;

    private ConversationManager()
    {
        _conversationsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "conversations");

        _conversationsIndexFile = Path.Combine(_conversationsPath, "index.json");

        Directory.CreateDirectory(_conversationsPath);
        LoadConversationsIndex();
    }

    public Conversation CreateConversation(string title = "新对话", string systemPrompt = "")
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = title,
            SystemPrompt = systemPrompt,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _conversations[conversation.Id] = conversation;
        _currentConversation = conversation;

        SaveConversation(conversation);
        SaveConversationsIndex();

        OnConversationCreated?.Invoke(conversation);
        OnCurrentConversationChanged?.Invoke(conversation);

        return conversation;
    }

    public Conversation? GetCurrentConversation()
    {
        return _currentConversation;
    }

    public void SetCurrentConversation(Guid conversationId)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            _currentConversation = conversation;
            LoadConversationMessages(conversation);
            OnCurrentConversationChanged?.Invoke(conversation);
        }
    }

    public void AddMessage(Guid conversationId, Message message)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            message.Timestamp = DateTime.Now;
            conversation.Messages.Add(message);
            conversation.UpdatedAt = DateTime.Now;

            if (conversation.Messages.Count == 1 && message.Role == "user")
            {
                conversation.Title = GenerateTitle(message.Content);
            }

            SaveConversation(conversation);
            OnConversationUpdated?.Invoke(conversation);
        }
    }

    public void AddMessageToCurrent(Message message)
    {
        if (_currentConversation != null)
        {
            AddMessage(_currentConversation.Id, message);
        }
    }

    public void UpdateMessage(Guid conversationId, int messageIndex, Message message)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            if (messageIndex >= 0 && messageIndex < conversation.Messages.Count)
            {
                conversation.Messages[messageIndex] = message;
                conversation.UpdatedAt = DateTime.Now;
                SaveConversation(conversation);
                OnConversationUpdated?.Invoke(conversation);
            }
        }
    }

    public void DeleteMessage(Guid conversationId, int messageIndex)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            if (messageIndex >= 0 && messageIndex < conversation.Messages.Count)
            {
                conversation.Messages.RemoveAt(messageIndex);
                conversation.UpdatedAt = DateTime.Now;
                SaveConversation(conversation);
                OnConversationUpdated?.Invoke(conversation);
            }
        }
    }

    public void PinMessage(Guid conversationId, int messageIndex, bool pinned)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            if (messageIndex >= 0 && messageIndex < conversation.Messages.Count)
            {
                conversation.Messages[messageIndex].IsPinned = pinned;
                conversation.UpdatedAt = DateTime.Now;
                SaveConversation(conversation);
                OnConversationUpdated?.Invoke(conversation);
            }
        }
    }

    public void DeleteConversation(Guid conversationId)
    {
        if (_conversations.Remove(conversationId))
        {
            var filePath = GetConversationFilePath(conversationId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            SaveConversationsIndex();
            OnConversationDeleted?.Invoke(conversationId);

            if (_currentConversation?.Id == conversationId)
            {
                _currentConversation = _conversations.Values.OrderByDescending(c => c.UpdatedAt).FirstOrDefault();
                OnCurrentConversationChanged?.Invoke(_currentConversation!);
            }
        }
    }

    public void ClearConversation(Guid conversationId)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.Messages.Clear();
            conversation.TotalTokens = 0;
            conversation.TotalCost = 0;
            conversation.UpdatedAt = DateTime.Now;
            SaveConversation(conversation);
            OnConversationUpdated?.Invoke(conversation);
        }
    }

    public void UpdateConversationTitle(Guid conversationId, string title)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.Title = title;
            conversation.UpdatedAt = DateTime.Now;
            SaveConversation(conversation);
            SaveConversationsIndex();
            OnConversationUpdated?.Invoke(conversation);
        }
    }

    public void UpdateConversationTags(Guid conversationId, List<string> tags)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.Tags = tags;
            conversation.UpdatedAt = DateTime.Now;
            SaveConversation(conversation);
            OnConversationUpdated?.Invoke(conversation);
        }
    }

    public void PinConversation(Guid conversationId, bool pinned)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.IsPinned = pinned;
            conversation.UpdatedAt = DateTime.Now;
            SaveConversation(conversation);
            SaveConversationsIndex();
            OnConversationUpdated?.Invoke(conversation);
        }
    }

    public List<Conversation> GetAllConversations()
    {
        return _conversations.Values
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .ToList();
    }

    public List<Conversation> SearchConversations(string query)
    {
        var results = new List<Conversation>();
        var lowerQuery = query.ToLower();

        foreach (var conversation in _conversations.Values)
        {
            if (conversation.Title.ToLower().Contains(lowerQuery) ||
                conversation.Tags.Any(t => t.ToLower().Contains(lowerQuery)))
            {
                results.Add(conversation);
                continue;
            }

            if (conversation.Messages.Any(m => m.Content.ToLower().Contains(lowerQuery)))
            {
                results.Add(conversation);
            }
        }

        return results.OrderByDescending(c => c.UpdatedAt).ToList();
    }

    public List<Conversation> GetConversationsByTag(string tag)
    {
        return _conversations.Values
            .Where(c => c.Tags.Contains(tag))
            .OrderByDescending(c => c.UpdatedAt)
            .ToList();
    }

    public List<Conversation> GetConversationsByDateRange(DateTime start, DateTime end)
    {
        return _conversations.Values
            .Where(c => c.CreatedAt >= start && c.CreatedAt <= end)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }

    public void UpdateConversationStats(Guid conversationId, int tokens, double cost)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.TotalTokens += tokens;
            conversation.TotalCost += cost;
            conversation.UpdatedAt = DateTime.Now;
            SaveConversation(conversation);
        }
    }

    private string GenerateTitle(string content)
    {
        var title = content.Trim();
        if (title.Length > 50)
        {
            title = title.Substring(0, 47) + "...";
        }
        return title.Replace("\n", " ").Replace("\r", "");
    }

    private string GetConversationFilePath(Guid conversationId)
    {
        return Path.Combine(_conversationsPath, $"{conversationId}.json");
    }

    private void LoadConversationsIndex()
    {
        try
        {
            if (File.Exists(_conversationsIndexFile))
            {
                var json = File.ReadAllText(_conversationsIndexFile);
                var index = JsonSerializer.Deserialize<List<ConversationIndexEntry>>(json);

                if (index != null)
                {
                    foreach (var entry in index)
                    {
                        _conversations[entry.Id] = new Conversation
                        {
                            Id = entry.Id,
                            Title = entry.Title,
                            CreatedAt = entry.CreatedAt,
                            UpdatedAt = entry.UpdatedAt,
                            IsPinned = entry.IsPinned,
                            Tags = entry.Tags ?? new List<string>(),
                            TotalTokens = entry.TotalTokens,
                            TotalCost = entry.TotalCost
                        };
                    }
                }
            }
        }
        catch { }
    }

    private void LoadConversationMessages(Conversation conversation)
    {
        try
        {
            var filePath = GetConversationFilePath(conversation.Id);
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var fullConversation = JsonSerializer.Deserialize<Conversation>(json);
                if (fullConversation != null)
                {
                    conversation.Messages = fullConversation.Messages;
                    conversation.SystemPrompt = fullConversation.SystemPrompt;
                    conversation.Model = fullConversation.Model;
                    conversation.Summary = fullConversation.Summary;
                }
            }
        }
        catch { }
    }

    private void SaveConversation(Conversation conversation)
    {
        try
        {
            var filePath = GetConversationFilePath(conversation.Id);
            var json = JsonSerializer.Serialize(conversation, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }
        catch { }
    }

    private void SaveConversationsIndex()
    {
        try
        {
            var index = _conversations.Values.Select(c => new ConversationIndexEntry
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsPinned = c.IsPinned,
                Tags = c.Tags,
                TotalTokens = c.TotalTokens,
                TotalCost = c.TotalCost
            }).ToList();

            var json = JsonSerializer.Serialize(index, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_conversationsIndexFile, json);
        }
        catch { }
    }

    public async Task ExportConversationAsync(Guid conversationId, string format, string outputPath)
    {
        if (!_conversations.TryGetValue(conversationId, out var conversation))
            return;

        LoadConversationMessages(conversation);

        var content = format.ToLower() switch
        {
            "json" => JsonSerializer.Serialize(conversation, new JsonSerializerOptions { WriteIndented = true }),
            "markdown" or "md" => ExportAsMarkdown(conversation),
            "txt" => ExportAsText(conversation),
            _ => JsonSerializer.Serialize(conversation, new JsonSerializerOptions { WriteIndented = true })
        };

        await File.WriteAllTextAsync(outputPath, content);
    }

    private string ExportAsMarkdown(Conversation conversation)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {conversation.Title}");
        sb.AppendLine($"创建时间: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(conversation.SystemPrompt))
        {
            sb.AppendLine("## System Prompt");
            sb.AppendLine(conversation.SystemPrompt);
            sb.AppendLine();
        }

        foreach (var msg in conversation.Messages)
        {
            sb.AppendLine($"### {msg.Role} ({msg.Timestamp:HH:mm:ss})");
            sb.AppendLine(msg.Content);
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine($"总Token: {conversation.TotalTokens} | 总费用: ${conversation.TotalCost:F4}");

        return sb.ToString();
    }

    private string ExportAsText(Conversation conversation)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== {conversation.Title} ===");
        sb.AppendLine();

        foreach (var msg in conversation.Messages)
        {
            sb.AppendLine($"[{msg.Role}] {msg.Timestamp:HH:mm:ss}");
            sb.AppendLine(msg.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public async Task ImportConversationAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var conversation = JsonSerializer.Deserialize<Conversation>(json);

            if (conversation != null)
            {
                conversation.Id = Guid.NewGuid();
                conversation.CreatedAt = DateTime.Now;
                conversation.UpdatedAt = DateTime.Now;

                _conversations[conversation.Id] = conversation;
                SaveConversation(conversation);
                SaveConversationsIndex();
                OnConversationCreated?.Invoke(conversation);
            }
        }
        catch { }
    }
}

internal class ConversationIndexEntry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPinned { get; set; }
    public List<string> Tags { get; set; } = new();
    public int TotalTokens { get; set; }
    public double TotalCost { get; set; }
}

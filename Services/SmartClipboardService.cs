using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public enum ContentType
{
    Text,
    Code,
    Json,
    Url,
    Email,
    PhoneNumber,
    FilePath,
    Image,
    Error,
    Unknown
}

public class ClipboardItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public ContentType Type { get; set; } = ContentType.Text;
    public string Source { get; set; } = string.Empty;
    public DateTime CopiedAt { get; set; } = DateTime.Now;
    public int AccessCount { get; set; }
    public bool IsFavorite { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Preview { get; set; }
    public string? ProcessedContent { get; set; }
    public int SizeBytes { get; set; }
}

public class ClipboardAction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ContentType? ApplicableType { get; set; }
    public Func<string, Task<string>>? Processor { get; set; }
    public bool IsAutoProcess { get; set; }
}

public sealed class SmartClipboardService : IDisposable
{
    private static readonly Lazy<SmartClipboardService> _instance = new(() => new SmartClipboardService());
    public static SmartClipboardService Instance => _instance.Value;

    private readonly List<ClipboardItem> _history = new();
    private readonly List<ClipboardAction> _actions = new();
    private readonly int _maxHistorySize = 500;
    private readonly string _historyFilePath;
    private Timer? _monitorTimer;
    private string _lastContent = string.Empty;
    private bool _isMonitoring;
    private bool _autoProcess;

    public event Action<ClipboardItem>? OnItemCopied;
    public event Action<ClipboardItem, ClipboardAction>? OnItemProcessed;
    public event Action<string>? OnError;

    public bool IsMonitoring => _isMonitoring;
    public bool AutoProcess
    {
        get => _autoProcess;
        set => _autoProcess = value;
    }

    private SmartClipboardService()
    {
        _historyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "clipboard_history.json");

        InitializeActions();
        LoadHistory();
    }

    private void InitializeActions()
    {
        RegisterAction(new ClipboardAction
        {
            Name = "格式化 JSON",
            Description = "自动美化 JSON 格式",
            ApplicableType = ContentType.Json,
            IsAutoProcess = true,
            Processor = async (content) =>
            {
                try
                {
                    var doc = JsonDocument.Parse(content);
                    return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                }
                catch { return content; }
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "代码格式化",
            Description = "格式化代码缩进",
            ApplicableType = ContentType.Code,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                return await Task.FromResult(content);
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "提取链接",
            Description = "从文本中提取所有 URL",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                var urls = System.Text.RegularExpressions.Regex.Matches(content, @"https?://[^\s]+")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value);
                return string.Join("\n", urls);
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "提取邮箱",
            Description = "从文本中提取所有邮箱地址",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                var emails = System.Text.RegularExpressions.Regex.Matches(content, @"[\w\.-]+@[\w\.-]+\.\w+")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value);
                return string.Join("\n", emails);
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "Base64 编码",
            Description = "将文本编码为 Base64",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                return Convert.ToBase64String(bytes);
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "Base64 解码",
            Description = "解码 Base64 文本",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                try
                {
                    var bytes = Convert.FromBase64String(content.Trim());
                    return Encoding.UTF8.GetString(bytes);
                }
                catch { return content; }
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "转大写",
            Description = "将文本转为大写",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) => content.ToUpper()
        });

        RegisterAction(new ClipboardAction
        {
            Name = "转小写",
            Description = "将文本转为小写",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) => content.ToLower()
        });

        RegisterAction(new ClipboardAction
        {
            Name = "去除空白",
            Description = "去除多余空白字符",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                return System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "统计字数",
            Description = "统计文本字数和字符数",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                var chars = content.Length;
                var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                var lines = content.Split(new[] { '\n' }, StringSplitOptions.None).Length;
                return $"字符数: {chars}\n单词数: {words}\n行数: {lines}";
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "AI 摘要",
            Description = "使用 AI 生成摘要",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                if (content.Length < 50) return content;
                var aiService = AIService.Instance;
                var response = await aiService.SendMessageAsync($"请用一句话总结以下内容：\n\n{content}");
                return response.Content;
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "AI 翻译",
            Description = "使用 AI 翻译为英文",
            ApplicableType = ContentType.Text,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                var aiService = AIService.Instance;
                var response = await aiService.SendMessageAsync($"请将以下内容翻译为英文：\n\n{content}");
                return response.Content;
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "解释代码",
            Description = "使用 AI 解释代码",
            ApplicableType = ContentType.Code,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                var aiService = AIService.Instance;
                var response = await aiService.SendMessageAsync($"请解释以下代码的功能：\n\n{content}");
                return response.Content;
            }
        });

        RegisterAction(new ClipboardAction
        {
            Name = "搜索错误解决方案",
            Description = "搜索错误信息的解决方案",
            ApplicableType = ContentType.Error,
            IsAutoProcess = false,
            Processor = async (content) =>
            {
                var aiService = AIService.Instance;
                var response = await aiService.SendMessageAsync($"以下错误信息的原因和解决方案是什么？\n\n{content}");
                return response.Content;
            }
        });
    }

    public void RegisterAction(ClipboardAction action)
    {
        _actions.Add(action);
    }

    public void RemoveAction(string actionId)
    {
        var action = _actions.FirstOrDefault(a => a.Id == actionId);
        if (action != null)
        {
            _actions.Remove(action);
        }
    }

    public List<ClipboardAction> GetActions(ContentType? type = null)
    {
        if (type == null)
            return new List<ClipboardAction>(_actions);

        return _actions.Where(a => a.ApplicableType == null || a.ApplicableType == type).ToList();
    }

    public void StartMonitoring(int intervalMs = 500)
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        _monitorTimer = new Timer(CheckClipboard, null, intervalMs, intervalMs);
    }

    public void StopMonitoring()
    {
        _isMonitoring = false;
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }

    private void CheckClipboard(object? state)
    {
        try
        {
            var content = GetClipboardText();

            if (!string.IsNullOrEmpty(content) && content != _lastContent)
            {
                _lastContent = content;
                ProcessClipboardContent(content);
            }
        }
        catch { }
    }

    private string GetClipboardText()
    {
        return string.Empty;
    }

    private void ProcessClipboardContent(string content)
    {
        var type = DetectContentType(content);
        var item = new ClipboardItem
        {
            Content = content,
            Type = type,
            SizeBytes = Encoding.UTF8.GetByteCount(content),
            Preview = GeneratePreview(content)
        };

        _history.Insert(0, item);

        if (_history.Count > _maxHistorySize)
        {
            _history.RemoveAt(_history.Count - 1);
        }

        OnItemCopied?.Invoke(item);

        if (_autoProcess)
        {
            var autoAction = _actions.FirstOrDefault(a => a.IsAutoProcess && a.ApplicableType == type);
            if (autoAction?.Processor != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        item.ProcessedContent = await autoAction.Processor(content);
                        OnItemProcessed?.Invoke(item, autoAction);
                    }
                    catch { }
                });
            }
        }

        SaveHistory();
    }

    public ContentType DetectContentType(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ContentType.Text;

        content = content.Trim();

        if (content.StartsWith("{") || content.StartsWith("["))
        {
            try
            {
                JsonDocument.Parse(content);
                return ContentType.Json;
            }
            catch { }
        }

        if (content.StartsWith("http://") || content.StartsWith("https://"))
        {
            return ContentType.Url;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
        {
            return ContentType.Email;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^\d{11}$"))
        {
            return ContentType.PhoneNumber;
        }

        if (content.Contains("public class") || content.Contains("def ") ||
            content.Contains("function") || content.Contains("import ") ||
            content.Contains("using ") || content.Contains("namespace "))
        {
            return ContentType.Code;
        }

        if (content.Contains("Exception") || content.Contains("Error") ||
            content.Contains("错误") || content.Contains("失败"))
        {
            return ContentType.Error;
        }

        if (System.IO.File.Exists(content) || System.IO.Directory.Exists(content))
        {
            return ContentType.FilePath;
        }

        return ContentType.Text;
    }

    private string GeneratePreview(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        var preview = content.Replace("\n", " ").Replace("\r", "");
        return preview.Length > 100 ? preview.Substring(0, 100) + "..." : preview;
    }

    public async Task<string> ProcessItemAsync(Guid itemId, string actionId)
    {
        var item = _history.FirstOrDefault(i => i.Id == itemId);
        var action = _actions.FirstOrDefault(a => a.Id == actionId);

        if (item == null || action?.Processor == null)
        {
            return string.Empty;
        }

        try
        {
            var result = await action.Processor(item.Content);
            item.ProcessedContent = result;
            item.AccessCount++;
            OnItemProcessed?.Invoke(item, action);
            return result;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"处理失败: {ex.Message}");
            return string.Empty;
        }
    }

    public List<ClipboardItem> GetHistory(int count = 50, ContentType? filterType = null)
    {
        var query = _history.AsEnumerable();

        if (filterType != null)
        {
            query = query.Where(i => i.Type == filterType);
        }

        return query.OrderByDescending(i => i.CopiedAt).Take(count).ToList();
    }

    public List<ClipboardItem> SearchHistory(string query)
    {
        var lowerQuery = query.ToLower();
        return _history
            .Where(i => i.Content.ToLower().Contains(lowerQuery) ||
                       i.Tags.Any(t => t.ToLower().Contains(lowerQuery)))
            .OrderByDescending(i => i.CopiedAt)
            .ToList();
    }

    public void ToggleFavorite(Guid itemId)
    {
        var item = _history.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.IsFavorite = !item.IsFavorite;
            SaveHistory();
        }
    }

    public void AddTag(Guid itemId, string tag)
    {
        var item = _history.FirstOrDefault(i => i.Id == itemId);
        if (item != null && !item.Tags.Contains(tag))
        {
            item.Tags.Add(tag);
            SaveHistory();
        }
    }

    public void RemoveTag(Guid itemId, string tag)
    {
        var item = _history.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.Tags.Remove(tag);
            SaveHistory();
        }
    }

    public void DeleteItem(Guid itemId)
    {
        var item = _history.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _history.Remove(item);
            SaveHistory();
        }
    }

    public void ClearHistory()
    {
        _history.Clear();
        SaveHistory();
    }

    public void ClearProcessedContent()
    {
        foreach (var item in _history)
        {
            item.ProcessedContent = null;
        }
        SaveHistory();
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var json = File.ReadAllText(_historyFilePath);
                var items = JsonSerializer.Deserialize<List<ClipboardItem>>(json);
                if (items != null)
                {
                    _history.Clear();
                    _history.AddRange(items);
                }
            }
        }
        catch { }
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyFilePath, json);
        }
        catch { }
    }

    public ClipboardStats GetStats()
    {
        return new ClipboardStats
        {
            TotalItems = _history.Count,
            FavoritesCount = _history.Count(i => i.IsFavorite),
            TodayCount = _history.Count(i => i.CopiedAt.Date == DateTime.Today),
            TypeDistribution = _history.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count()),
            TotalSizeBytes = _history.Sum(i => i.SizeBytes)
        };
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}

public class ClipboardStats
{
    public int TotalItems { get; set; }
    public int FavoritesCount { get; set; }
    public int TodayCount { get; set; }
    public Dictionary<ContentType, int> TypeDistribution { get; set; } = new();
    public long TotalSizeBytes { get; set; }
}

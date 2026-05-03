using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public class HotkeyDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultKey { get; set; } = string.Empty;
    public string CurrentKey { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public bool IsGlobal { get; set; }
    public Action? Callback { get; set; }
}

public class HotkeyProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Hotkeys { get; set; } = new();
    public bool IsDefault { get; set; }
}

public sealed class HotkeyService
{
    private static readonly Lazy<HotkeyService> _instance = new(() => new HotkeyService());
    public static HotkeyService Instance => _instance.Value;

    private readonly Dictionary<string, HotkeyDefinition> _hotkeys = new();
    private readonly List<HotkeyProfile> _profiles = new();
    private HotkeyProfile _activeProfile;
    private readonly string _configPath;

    public event Action<string, string>? OnHotkeyTriggered;
    public event Action? OnHotkeysChanged;

    private HotkeyService()
    {
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "hotkeys.json");

        _activeProfile = new HotkeyProfile { Name = "Default", IsDefault = true };
        InitializeDefaultHotkeys();
        LoadConfiguration();
    }

    private void InitializeDefaultHotkeys()
    {
        RegisterHotkey(new HotkeyDefinition
        {
            Id = "new_conversation",
            Name = "新建对话",
            Description = "创建一个新的 AI 对话",
            DefaultKey = "Ctrl+N",
            CurrentKey = "Ctrl+N",
            Category = "AI Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "send_message",
            Name = "发送消息",
            Description = "发送当前输入的消息",
            DefaultKey = "Ctrl+Enter",
            CurrentKey = "Ctrl+Enter",
            Category = "AI Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "clear_chat",
            Name = "清空对话",
            Description = "清空当前对话内容",
            DefaultKey = "Ctrl+Shift+C",
            CurrentKey = "Ctrl+Shift+C",
            Category = "AI Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "copy_response",
            Name = "复制回复",
            Description = "复制 AI 的最后一条回复",
            DefaultKey = "Ctrl+Shift+Y",
            CurrentKey = "Ctrl+Shift+Y",
            Category = "AI Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "toggle_streaming",
            Name = "切换流式输出",
            Description = "开启/关闭流式输出模式",
            DefaultKey = "Ctrl+Shift+S",
            CurrentKey = "Ctrl+Shift+S",
            Category = "AI Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "format_json",
            Name = "格式化 JSON",
            Description = "打开 JSON 格式化工具",
            DefaultKey = "Ctrl+J",
            CurrentKey = "Ctrl+J",
            Category = "Developer Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "base64_encode",
            Name = "Base64 编码",
            Description = "打开 Base64 编码工具",
            DefaultKey = "Ctrl+Shift+B",
            CurrentKey = "Ctrl+Shift+B",
            Category = "Developer Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "hash_calculator",
            Name = "哈希计算",
            Description = "打开哈希计算工具",
            DefaultKey = "Ctrl+H",
            CurrentKey = "Ctrl+H",
            Category = "Developer Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "timestamp",
            Name = "时间戳转换",
            Description = "打开时间戳转换工具",
            DefaultKey = "Ctrl+T",
            CurrentKey = "Ctrl+T",
            Category = "Developer Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "uuid_generate",
            Name = "生成 UUID",
            Description = "生成新的 UUID",
            DefaultKey = "Ctrl+Shift+U",
            CurrentKey = "Ctrl+Shift+U",
            Category = "Developer Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "code_sandbox",
            Name = "代码沙盒",
            Description = "打开代码沙盒",
            DefaultKey = "Ctrl+Shift+R",
            CurrentKey = "Ctrl+Shift+R",
            Category = "Developer Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "knowledge_base",
            Name = "知识库",
            Description = "打开知识库",
            DefaultKey = "Ctrl+K",
            CurrentKey = "Ctrl+K",
            Category = "AI Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "workflow",
            Name = "工作流",
            Description = "打开工作流引擎",
            DefaultKey = "Ctrl+W",
            CurrentKey = "Ctrl+W",
            Category = "AI Tools"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "settings",
            Name = "设置",
            Description = "打开设置",
            DefaultKey = "Ctrl+,",
            CurrentKey = "Ctrl+,",
            Category = "General"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "search",
            Name = "搜索",
            Description = "打开搜索",
            DefaultKey = "Ctrl+F",
            CurrentKey = "Ctrl+F",
            Category = "General"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "toggle_sidebar",
            Name = "切换侧边栏",
            Description = "显示/隐藏侧边栏",
            DefaultKey = "Ctrl+B",
            CurrentKey = "Ctrl+B",
            Category = "General"
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "quick_action",
            Name = "快速操作",
            Description = "打开快速操作面板",
            DefaultKey = "Ctrl+Shift+P",
            CurrentKey = "Ctrl+Shift+P",
            Category = "General",
            IsGlobal = true
        });

        RegisterHotkey(new HotkeyDefinition
        {
            Id = "clipboard_history",
            Name = "剪贴板历史",
            Description = "显示剪贴板历史",
            DefaultKey = "Ctrl+Shift+V",
            CurrentKey = "Ctrl+Shift+V",
            Category = "General"
        });
    }

    public void RegisterHotkey(HotkeyDefinition hotkey)
    {
        _hotkeys[hotkey.Id] = hotkey;
    }

    public void UnregisterHotkey(string id)
    {
        _hotkeys.Remove(id);
    }

    public void UpdateHotkey(string id, string newKey)
    {
        if (_hotkeys.TryGetValue(id, out var hotkey))
        {
            hotkey.CurrentKey = newKey;
            SaveConfiguration();
            OnHotkeysChanged?.Invoke();
        }
    }

    public void ResetHotkey(string id)
    {
        if (_hotkeys.TryGetValue(id, out var hotkey))
        {
            hotkey.CurrentKey = hotkey.DefaultKey;
            SaveConfiguration();
            OnHotkeysChanged?.Invoke();
        }
    }

    public void ResetAllHotkeys()
    {
        foreach (var hotkey in _hotkeys.Values)
        {
            hotkey.CurrentKey = hotkey.DefaultKey;
        }
        SaveConfiguration();
        OnHotkeysChanged?.Invoke();
    }

    public HotkeyDefinition? GetHotkey(string id)
    {
        return _hotkeys.GetValueOrDefault(id);
    }

    public List<HotkeyDefinition> GetAllHotkeys()
    {
        return new List<HotkeyDefinition>(_hotkeys.Values);
    }

    public List<HotkeyDefinition> GetHotkeysByCategory(string category)
    {
        return _hotkeys.Values.Where(h => h.Category == category).ToList();
    }

    public List<string> GetCategories()
    {
        return _hotkeys.Values.Select(h => h.Category).Distinct().ToList();
    }

    public bool ProcessKeyEvent(string keyCombination)
    {
        foreach (var hotkey in _hotkeys.Values)
        {
            if (hotkey.CurrentKey.Equals(keyCombination, StringComparison.OrdinalIgnoreCase))
            {
                OnHotkeyTriggered?.Invoke(hotkey.Id, hotkey.Name);
                hotkey.Callback?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void SetCallback(string id, Action callback)
    {
        if (_hotkeys.TryGetValue(id, out var hotkey))
        {
            hotkey.Callback = callback;
        }
    }

    public HotkeyProfile CreateProfile(string name)
    {
        var profile = new HotkeyProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Hotkeys = _hotkeys.ToDictionary(h => h.Key, h => h.Value.CurrentKey)
        };
        _profiles.Add(profile);
        SaveConfiguration();
        return profile;
    }

    public void DeleteProfile(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile != null && !profile.IsDefault)
        {
            _profiles.Remove(profile);
            SaveConfiguration();
        }
    }

    public void ActivateProfile(string profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile != null)
        {
            _activeProfile = profile;
            foreach (var (id, key) in profile.Hotkeys)
            {
                if (_hotkeys.TryGetValue(id, out var hotkey))
                {
                    hotkey.CurrentKey = key;
                }
            }
            OnHotkeysChanged?.Invoke();
        }
    }

    public List<HotkeyProfile> GetProfiles()
    {
        return new List<HotkeyProfile>(_profiles);
    }

    public HotkeyProfile GetActiveProfile()
    {
        return _activeProfile;
    }

    public string ParseKeyCombination(bool ctrl, bool shift, bool alt, string key)
    {
        var parts = new List<string>();
        if (ctrl) parts.Add("Ctrl");
        if (shift) parts.Add("Shift");
        if (alt) parts.Add("Alt");
        if (!string.IsNullOrEmpty(key)) parts.Add(key.ToUpper());
        return string.Join("+", parts);
    }

    public bool IsKeyConflict(string newKey, string? excludeId = null)
    {
        return _hotkeys.Values.Any(h => 
            h.Id != excludeId && 
            h.CurrentKey.Equals(newKey, StringComparison.OrdinalIgnoreCase));
    }

    public List<HotkeyDefinition> FindConflicts()
    {
        var conflicts = new List<HotkeyDefinition>();
        var seen = new Dictionary<string, HotkeyDefinition>();

        foreach (var hotkey in _hotkeys.Values)
        {
            if (seen.TryGetValue(hotkey.CurrentKey, out var existing))
            {
                if (!conflicts.Contains(existing)) conflicts.Add(existing);
                conflicts.Add(hotkey);
            }
            else
            {
                seen[hotkey.CurrentKey] = hotkey;
            }
        }

        return conflicts;
    }

    private void LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<HotkeyConfig>(json);
                
                if (config != null)
                {
                    foreach (var (id, key) in config.Hotkeys)
                    {
                        if (_hotkeys.TryGetValue(id, out var hotkey))
                        {
                            hotkey.CurrentKey = key;
                        }
                    }

                    if (config.Profiles != null)
                    {
                        _profiles.Clear();
                        _profiles.AddRange(config.Profiles);
                    }

                    if (!string.IsNullOrEmpty(config.ActiveProfileId))
                    {
                        _activeProfile = _profiles.FirstOrDefault(p => p.Id == config.ActiveProfileId) ?? _activeProfile;
                    }
                }
            }
        }
        catch { }
    }

    private void SaveConfiguration()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var config = new HotkeyConfig
            {
                Hotkeys = _hotkeys.ToDictionary(h => h.Key, h => h.Value.CurrentKey),
                Profiles = _profiles,
                ActiveProfileId = _activeProfile.Id
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch { }
    }

    public void ExportProfile(string profileId, string outputPath)
    {
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile == null) return;

        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }

    public void ImportProfile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var profile = JsonSerializer.Deserialize<HotkeyProfile>(json);
            if (profile != null)
            {
                profile.Id = Guid.NewGuid().ToString();
                profile.IsDefault = false;
                _profiles.Add(profile);
                SaveConfiguration();
            }
        }
        catch { }
    }
}

internal class HotkeyConfig
{
    public Dictionary<string, string> Hotkeys { get; set; } = new();
    public List<HotkeyProfile>? Profiles { get; set; }
    public string? ActiveProfileId { get; set; }
}

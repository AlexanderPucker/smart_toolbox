using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class HotkeySettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private HotkeyItem? _selectedHotkey;

    [ObservableProperty]
    private string _currentKeyDisplay = string.Empty;

    [ObservableProperty]
    private bool _isRecordingKey;

    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<HotkeyItem> Hotkeys { get; } = new();
    public ObservableCollection<HotkeyProfileItem> Profiles { get; } = new();

    private readonly HotkeyService _hotkeyService;
    private string? _pendingHotkeyId;

    public HotkeySettingsViewModel()
    {
        _hotkeyService = HotkeyService.Instance;
        _hotkeyService.OnHotkeyTriggered += OnHotkeyTriggered;
        _hotkeyService.OnHotkeysChanged += OnHotkeysChanged;

        LoadCategories();
        LoadHotkeys();
        LoadProfiles();
    }

    private void OnHotkeyTriggered(string id, string name)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = $"触发快捷键: {name}";
        });
    }

    private void OnHotkeysChanged()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => LoadHotkeys());
    }

    private void LoadCategories()
    {
        Categories.Clear();
        Categories.Add("全部");
        foreach (var category in _hotkeyService.GetCategories())
        {
            Categories.Add(category);
        }
    }

    private void LoadHotkeys()
    {
        Hotkeys.Clear();
        foreach (var hotkey in _hotkeyService.GetAllHotkeys())
        {
            Hotkeys.Add(new HotkeyItem
            {
                Id = hotkey.Id,
                Name = hotkey.Name,
                Description = hotkey.Description,
                Category = hotkey.Category,
                CurrentKey = hotkey.CurrentKey,
                DefaultKey = hotkey.DefaultKey,
                IsGlobal = hotkey.IsGlobal
            });
        }
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _hotkeyService.GetProfiles())
        {
            Profiles.Add(new HotkeyProfileItem
            {
                Id = profile.Id,
                Name = profile.Name,
                IsDefault = profile.IsDefault,
                IsActive = profile.Id == _hotkeyService.GetActiveProfile().Id
            });
        }
    }

    [RelayCommand]
    private void FilterByCategory(string category)
    {
        Hotkeys.Clear();
        var hotkeys = category == "全部"
            ? _hotkeyService.GetAllHotkeys()
            : _hotkeyService.GetHotkeysByCategory(category);

        foreach (var hotkey in hotkeys)
        {
            Hotkeys.Add(new HotkeyItem
            {
                Id = hotkey.Id,
                Name = hotkey.Name,
                Description = hotkey.Description,
                Category = hotkey.Category,
                CurrentKey = hotkey.CurrentKey,
                DefaultKey = hotkey.DefaultKey,
                IsGlobal = hotkey.IsGlobal
            });
        }
    }

    [RelayCommand]
    private void StartRecordingKey()
    {
        if (SelectedHotkey == null) return;

        _pendingHotkeyId = SelectedHotkey.Id;
        IsRecordingKey = true;
        CurrentKeyDisplay = "请按下新的快捷键...";
    }

    public void RecordKey(bool ctrl, bool shift, bool alt, string key)
    {
        if (!IsRecordingKey || string.IsNullOrEmpty(_pendingHotkeyId)) return;

        var keyCombo = _hotkeyService.ParseKeyCombination(ctrl, shift, alt, key);

        if (_hotkeyService.IsKeyConflict(keyCombo, _pendingHotkeyId))
        {
            StatusMessage = $"快捷键 {keyCombo} 已被使用";
            return;
        }

        _hotkeyService.UpdateHotkey(_pendingHotkeyId, keyCombo);
        StatusMessage = $"已更新快捷键: {keyCombo}";

        IsRecordingKey = false;
        _pendingHotkeyId = null;
        CurrentKeyDisplay = string.Empty;
        LoadHotkeys();
    }

    [RelayCommand]
    private void ResetHotkey()
    {
        if (SelectedHotkey == null) return;

        _hotkeyService.ResetHotkey(SelectedHotkey.Id);
        StatusMessage = "已重置为默认快捷键";
        LoadHotkeys();
    }

    [RelayCommand]
    private void ResetAllHotkeys()
    {
        _hotkeyService.ResetAllHotkeys();
        StatusMessage = "已重置所有快捷键";
        LoadHotkeys();
    }

    [RelayCommand]
    private void CreateProfile()
    {
        var profile = _hotkeyService.CreateProfile($"配置文件 {Profiles.Count + 1}");
        Profiles.Add(new HotkeyProfileItem
        {
            Id = profile.Id,
            Name = profile.Name,
            IsDefault = false,
            IsActive = false
        });
        StatusMessage = "已创建新配置文件";
    }

    [RelayCommand]
    private void ActivateProfile(HotkeyProfileItem? profile)
    {
        if (profile == null) return;

        _hotkeyService.ActivateProfile(profile.Id);

        foreach (var p in Profiles)
        {
            p.IsActive = p.Id == profile.Id;
        }

        LoadHotkeys();
        StatusMessage = $"已激活配置文件: {profile.Name}";
    }

    [RelayCommand]
    private void DeleteProfile(HotkeyProfileItem? profile)
    {
        if (profile == null || profile.IsDefault) return;

        _hotkeyService.DeleteProfile(profile.Id);
        Profiles.Remove(profile);
        StatusMessage = "已删除配置文件";
    }

    [RelayCommand]
    private void FindConflicts()
    {
        var conflicts = _hotkeyService.FindConflicts();
        if (conflicts.Any())
        {
            StatusMessage = $"发现 {conflicts.Count} 个快捷键冲突";
            Hotkeys.Clear();
            foreach (var conflict in conflicts)
            {
                Hotkeys.Add(new HotkeyItem
                {
                    Id = conflict.Id,
                    Name = conflict.Name,
                    Description = conflict.Description,
                    Category = conflict.Category,
                    CurrentKey = conflict.CurrentKey,
                    DefaultKey = conflict.DefaultKey,
                    IsGlobal = conflict.IsGlobal
                });
            }
        }
        else
        {
            StatusMessage = "没有发现快捷键冲突";
        }
    }
}

public class HotkeyItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CurrentKey { get; set; } = string.Empty;
    public string DefaultKey { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
}

public class HotkeyProfileItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SmartToolbox.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

public partial class FileToolView : UserControl
{
    private readonly ObservableCollection<string> _originalFiles = new();
    private readonly ObservableCollection<string> _previewFiles = new();
    private readonly List<FileInfo> _fileInfos = new();
    private string _currentFolderPath = "";

    public FileToolView()
    {
        InitializeComponent();
        
        var originalFilesList = this.FindControl<ListBox>("OriginalFilesList");
        var previewFilesList = this.FindControl<ListBox>("PreviewFilesList");
        
        if (originalFilesList != null)
            originalFilesList.ItemsSource = _originalFiles;
        
        if (previewFilesList != null)
            previewFilesList.ItemsSource = _previewFiles;
            
        // 初始化文件过滤器下拉框
        InitializeFileFilterComboBox();
    }

    private void InitializeFileFilterComboBox()
    {
        var fileFilterComboBox = this.FindControl<ComboBox>("FileFilterComboBox");
        if (fileFilterComboBox != null)
        {
            fileFilterComboBox.ItemsSource = FileFilterConfig.CommonFilters;
            fileFilterComboBox.SelectedIndex = 0; // 默认选择"所有文件"
        }
    }

    private void OnFileFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        // 当下拉框选择发生变化时，自动刷新文件列表
        if (!string.IsNullOrEmpty(_currentFolderPath))
        {
            _ = RefreshFileListInternal();
        }
    }

    private async void SelectFolder(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window window) return;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择文件夹",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            _currentFolderPath = folders[0].Path.LocalPath;
            var folderPathTextBox = this.FindControl<TextBox>("FolderPathTextBox");
            if (folderPathTextBox != null)
                folderPathTextBox.Text = _currentFolderPath;
            
            await RefreshFileListInternal();
        }
    }

    private async void RefreshFileList(object? sender, RoutedEventArgs e)
    {
        await RefreshFileListInternal();
    }

    private Task RefreshFileListInternal()
    {
        if (string.IsNullOrEmpty(_currentFolderPath) || !Directory.Exists(_currentFolderPath))
        {
            UpdateStatus("请先选择一个有效的文件夹");
            return Task.CompletedTask;
        }

        try
        {
            var fileFilterComboBox = this.FindControl<ComboBox>("FileFilterComboBox");
            string filter = "*.*";
            
            if (fileFilterComboBox?.SelectedItem is FileFilterItem selectedFilter)
            {
                filter = selectedFilter.Pattern;
            }
            
            // 解析过滤器，支持多个模式（用逗号分隔）
            var patterns = filter.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToArray();

            _fileInfos.Clear();
            
            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(_currentFolderPath, pattern, SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (!_fileInfos.Any(f => f.FullName == fileInfo.FullName))
                    {
                        _fileInfos.Add(fileInfo);
                    }
                }
            }

            // 按文件名排序
            _fileInfos.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

            // 更新原文件名列表
            _originalFiles.Clear();
            foreach (var file in _fileInfos)
            {
                _originalFiles.Add(file.Name);
            }

            // 重置预览
            ResetPreviewInternal();
            
            UpdateStatus($"已加载 {_fileInfos.Count} 个文件");
            UpdateFileCount();
        }
        catch (Exception ex)
        {
            UpdateStatus($"读取文件列表失败: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    private void ConvertToUpperCase(object? sender, RoutedEventArgs e)
    {
        ApplyToSelectedOrAll(fileName =>
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return nameWithoutExt.ToUpper() + extension;
        });
        UpdateStatus("已应用大写转换");
    }

    private void ConvertToLowerCase(object? sender, RoutedEventArgs e)
    {
        ApplyToSelectedOrAll(fileName =>
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return nameWithoutExt.ToLower() + extension;
        });
        UpdateStatus("已应用小写转换");
    }

    private void ConvertToTitleCase(object? sender, RoutedEventArgs e)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        ApplyToSelectedOrAll(fileName =>
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return textInfo.ToTitleCase(nameWithoutExt.ToLower()) + extension;
        });
        UpdateStatus("已应用首字母大写转换");
    }

    private void RemoveSpecificText(object? sender, RoutedEventArgs e)
    {
        var removeTextBox = this.FindControl<TextBox>("RemoveTextBox");
        var textToRemove = removeTextBox?.Text;
        
        if (string.IsNullOrEmpty(textToRemove))
        {
            UpdateStatus("请输入要删除的文本");
            return;
        }

        ApplyToSelectedOrAll(fileName =>
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var newName = nameWithoutExt.Replace(textToRemove, "");
            return newName + extension;
        });
        
        UpdateStatus($"已删除文本: {textToRemove}");
    }

    private void AddPrefix(object? sender, RoutedEventArgs e)
    {
        var prefixTextBox = this.FindControl<TextBox>("PrefixTextBox");
        var prefix = prefixTextBox?.Text;
        
        if (string.IsNullOrEmpty(prefix))
        {
            UpdateStatus("请输入前缀文本");
            return;
        }

        ApplyToSelectedOrAll(fileName =>
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return prefix + nameWithoutExt + extension;
        });
        
        UpdateStatus($"已添加前缀: {prefix}");
    }

    private void AddSuffix(object? sender, RoutedEventArgs e)
    {
        var suffixTextBox = this.FindControl<TextBox>("SuffixTextBox");
        var suffix = suffixTextBox?.Text;
        
        if (string.IsNullOrEmpty(suffix))
        {
            UpdateStatus("请输入后缀文本");
            return;
        }

        ApplyToSelectedOrAll(fileName =>
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return nameWithoutExt + suffix + extension;
        });
        
        UpdateStatus($"已添加后缀: {suffix}");
    }

    private void FindAndReplace(object? sender, RoutedEventArgs e)
    {
        var findTextBox = this.FindControl<TextBox>("FindTextBox");
        var replaceTextBox = this.FindControl<TextBox>("ReplaceTextBox");
        
        var findText = findTextBox?.Text;
        var replaceText = replaceTextBox?.Text ?? "";
        
        if (string.IsNullOrEmpty(findText))
        {
            UpdateStatus("请输入要查找的文本");
            return;
        }

        ApplyToSelectedOrAll(fileName =>
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var newName = nameWithoutExt.Replace(findText, replaceText);
            return newName + extension;
        });
        
        UpdateStatus($"已替换: '{findText}' -> '{replaceText}'");
    }

    private void ApplyToSelectedOrAll(Func<string, string> transform)
    {
        var originalFilesList = this.FindControl<ListBox>("OriginalFilesList");
        var selectedIndices = originalFilesList?.SelectedItems?.Cast<string>()
            .Select(item => _originalFiles.IndexOf(item))
            .Where(index => index >= 0)
            .ToList() ?? new List<int>();

        // 如果没有选择任何文件，则应用到所有文件
        if (selectedIndices.Count == 0)
        {
            selectedIndices = Enumerable.Range(0, _originalFiles.Count).ToList();
        }

        _previewFiles.Clear();
        for (int i = 0; i < _originalFiles.Count; i++)
        {
            if (selectedIndices.Contains(i))
            {
                var newFileName = transform(_originalFiles[i]);
                _previewFiles.Add(newFileName);
            }
            else
            {
                _previewFiles.Add(_originalFiles[i]);
            }
        }
    }

    private async void ApplyRename(object? sender, RoutedEventArgs e)
    {
        if (_fileInfos.Count == 0)
        {
            UpdateStatus("没有文件可以重命名");
            return;
        }

        if (_previewFiles.Count != _originalFiles.Count)
        {
            UpdateStatus("预览数据不匹配，请重新生成预览");
            return;
        }

        int successCount = 0;
        int errorCount = 0;
        var errors = new List<string>();

        try
        {
            for (int i = 0; i < _fileInfos.Count; i++)
            {
                var originalFile = _fileInfos[i];
                var newFileName = _previewFiles[i];
                
                // 跳过未更改的文件
                if (originalFile.Name == newFileName)
                    continue;

                var newFilePath = Path.Combine(originalFile.DirectoryName!, newFileName);
                
                // 检查目标文件是否已存在
                if (File.Exists(newFilePath))
                {
                    errors.Add($"目标文件已存在: {newFileName}");
                    errorCount++;
                    continue;
                }

                try
                {
                    File.Move(originalFile.FullName, newFilePath);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{originalFile.Name}: {ex.Message}");
                    errorCount++;
                }
            }

            // 刷新文件列表
            await RefreshFileListInternal();
            
            var statusMessage = $"重命名完成 - 成功: {successCount}, 错误: {errorCount}";
            if (errors.Count > 0 && errors.Count <= 5)
            {
                statusMessage += $" (错误: {string.Join("; ", errors)})";
            }
            else if (errors.Count > 5)
            {
                statusMessage += $" (显示前5个错误: {string.Join("; ", errors.Take(5))})";
            }
            
            UpdateStatus(statusMessage);
        }
        catch (Exception ex)
        {
            UpdateStatus($"重命名过程中发生错误: {ex.Message}");
        }
    }

    private void ResetPreview(object? sender, RoutedEventArgs e)
    {
        ResetPreviewInternal();
        UpdateStatus("预览已重置");
    }

    private void ResetPreviewInternal()
    {
        _previewFiles.Clear();
        foreach (var fileName in _originalFiles)
        {
            _previewFiles.Add(fileName);
        }
    }

    private void ClearAll(object? sender, RoutedEventArgs e)
    {
        _originalFiles.Clear();
        _previewFiles.Clear();
        _fileInfos.Clear();
        _currentFolderPath = "";
        
        var folderPathTextBox = this.FindControl<TextBox>("FolderPathTextBox");
        if (folderPathTextBox != null)
            folderPathTextBox.Text = "";
            
        UpdateStatus("已清空所有内容");
        UpdateFileCount();
    }

    private void UpdateStatus(string message)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
            statusText.Text = message;
    }

    private void UpdateFileCount()
    {
        var fileCountText = this.FindControl<TextBlock>("FileCountText");
        if (fileCountText != null)
            fileCountText.Text = $"文件数量: {_fileInfos.Count}";
    }
} 
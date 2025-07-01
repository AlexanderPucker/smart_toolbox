using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SmartToolbox.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

public partial class FileMoverView : UserControl
{
    // 移动文件功能相关字段
    private readonly ObservableCollection<string> _sourceFiles = new();
    private readonly ObservableCollection<string> _movePreviewFiles = new();
    private readonly List<FileInfo> _filesToMove = new();
    private string _sourceFolderPath = "";
    private string _targetFolderPath = "";
    
    // 滚动同步相关字段
    private ScrollViewer? _leftScrollViewer;
    private ScrollViewer? _rightScrollViewer;
    private bool _isScrollSyncEnabled = true;

    public FileMoverView()
    {
        InitializeComponent();
        
        // 初始化源文件列表
        var sourceFilesList = this.FindControl<ListBox>("SourceFilesList");
        if (sourceFilesList != null)
            sourceFilesList.ItemsSource = _sourceFiles;
            
        // 初始化移动文件预览列表
        var movePreviewList = this.FindControl<ListBox>("MovePreviewList");
        if (movePreviewList != null)
            movePreviewList.ItemsSource = _movePreviewFiles;
            
        // 初始化移动文件过滤器下拉框
        InitializeMoveFileFilterComboBox();
        
        // 初始化滚动同步
        InitializeScrollSync();
    }

    #region 移动文件功能

    private void InitializeMoveFileFilterComboBox()
    {
        var moveFileFilterComboBox = this.FindControl<ComboBox>("MoveFileFilterComboBox");
        if (moveFileFilterComboBox != null)
        {
            moveFileFilterComboBox.ItemsSource = FileFilterConfig.CommonFilters;
            moveFileFilterComboBox.SelectedIndex = 0; // 默认选择"所有文件"
        }
    }

    private async void SelectSourceFolder(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window window) return;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择源文件夹",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            _sourceFolderPath = folders[0].Path.LocalPath;
            var sourceFolderTextBox = this.FindControl<TextBox>("SourceFolderTextBox");
            if (sourceFolderTextBox != null)
                sourceFolderTextBox.Text = _sourceFolderPath;
            
            UpdateStatus("已选择源文件夹: " + _sourceFolderPath);
        }
    }

    private async void SelectTargetFolder(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window window) return;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择目标文件夹",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            _targetFolderPath = folders[0].Path.LocalPath;
            var targetFolderTextBox = this.FindControl<TextBox>("TargetFolderTextBox");
            if (targetFolderTextBox != null)
                targetFolderTextBox.Text = _targetFolderPath;
            
            UpdateStatus("已选择目标文件夹: " + _targetFolderPath);
        }
    }

    private void PreviewMoveFiles(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_sourceFolderPath) || !Directory.Exists(_sourceFolderPath))
        {
            UpdateStatus("请先选择一个有效的源文件夹");
            return;
        }

        if (string.IsNullOrEmpty(_targetFolderPath) || !Directory.Exists(_targetFolderPath))
        {
            UpdateStatus("请先选择一个有效的目标文件夹");
            return;
        }

        try
        {
            var moveFileFilterComboBox = this.FindControl<ComboBox>("MoveFileFilterComboBox");
            var includeSubfoldersCheckBox = this.FindControl<CheckBox>("IncludeSubfoldersCheckBox");
            
            string filter = "*.*";
            if (moveFileFilterComboBox?.SelectedItem is FileFilterItem selectedFilter)
            {
                filter = selectedFilter.Pattern;
            }

            bool includeSubfolders = includeSubfoldersCheckBox?.IsChecked ?? true;
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 解析过滤器，支持多个模式（用逗号分隔）
            var patterns = filter.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToArray();

            _filesToMove.Clear();
            _sourceFiles.Clear();
            _movePreviewFiles.Clear();

            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(_sourceFolderPath, pattern, searchOption);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (!_filesToMove.Any(f => f.FullName == fileInfo.FullName))
                    {
                        _filesToMove.Add(fileInfo);
                    }
                }
            }

            // 按文件路径排序
            _filesToMove.Sort((x, y) => string.Compare(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase));

            // 更新源文件列表和预览列表
            foreach (var file in _filesToMove)
            {
                // 左侧显示：相对于源文件夹的路径
                var relativePath = Path.GetRelativePath(_sourceFolderPath, file.FullName);
                _sourceFiles.Add(relativePath);
                
                // 右侧显示：目标文件夹+文件名
                var targetFileName = file.Name;
                var targetDisplay = $"{Path.GetFileName(_targetFolderPath)}\\{targetFileName}";
                _movePreviewFiles.Add(targetDisplay);
            }

            UpdateStatus($"预览完成，找到 {_filesToMove.Count} 个文件待移动");
        }
        catch (Exception ex)
        {
            UpdateStatus($"预览文件失败: {ex.Message}");
        }
    }

    private void ExecuteMoveFiles(object? sender, RoutedEventArgs e)
    {
        if (_filesToMove.Count == 0)
        {
            UpdateStatus("没有找到要移动的文件，请先点击'预览移动'");
            return;
        }

        if (string.IsNullOrEmpty(_targetFolderPath) || !Directory.Exists(_targetFolderPath))
        {
            UpdateStatus("目标文件夹无效");
            return;
        }

        try
        {
            // 确保目标文件夹存在
            Directory.CreateDirectory(_targetFolderPath);

            int successCount = 0;
            var errors = new List<string>();

            foreach (var file in _filesToMove)
            {
                try
                {
                    var targetFilePath = Path.Combine(_targetFolderPath, file.Name);
                    
                    // 检查目标文件是否已存在
                    if (File.Exists(targetFilePath))
                    {
                        // 生成唯一的文件名
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                        var extension = Path.GetExtension(file.Name);
                        int counter = 1;
                        
                        while (File.Exists(targetFilePath))
                        {
                            var newName = $"{nameWithoutExt}_{counter}{extension}";
                            targetFilePath = Path.Combine(_targetFolderPath, newName);
                            counter++;
                        }
                    }

                    // 移动文件
                    File.Move(file.FullName, targetFilePath);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.Name}: {ex.Message}");
                }
            }

            // 更新状态
            var statusMessage = $"移动完成: 成功 {successCount} 个";
            if (errors.Count > 0)
                statusMessage += $", 失败 {errors.Count} 个";

            UpdateStatus(statusMessage);

            if (errors.Count > 0)
            {
                // 可以在这里显示详细错误信息，暂时只显示第一个错误
                UpdateStatus($"部分文件移动失败: {errors[0]}");
            }

            // 清除预览
            ClearMovePreviewInternal();
        }
        catch (Exception ex)
        {
            UpdateStatus($"移动文件失败: {ex.Message}");
        }
    }

    private void ClearMovePreview(object? sender, RoutedEventArgs e)
    {
        ClearMovePreviewInternal();
    }

    private void ClearMovePreviewInternal()
    {
        _filesToMove.Clear();
        _sourceFiles.Clear();
        _movePreviewFiles.Clear();
        UpdateStatus("已清除移动预览");
    }

    #endregion

    #region 滚动同步

    private void InitializeScrollSync()
    {
        // 获取滚动视图控件的引用
        _leftScrollViewer = this.FindControl<ScrollViewer>("LeftScrollViewer");
        _rightScrollViewer = this.FindControl<ScrollViewer>("RightScrollViewer");

        // 绑定滚动事件
        if (_leftScrollViewer != null)
        {
            _leftScrollViewer.ScrollChanged += OnLeftScrollChanged;
        }

        if (_rightScrollViewer != null)
        {
            _rightScrollViewer.ScrollChanged += OnRightScrollChanged;
        }
    }

    private void OnLeftScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (!_isScrollSyncEnabled || _rightScrollViewer == null) return;

        _isScrollSyncEnabled = false; // 防止递归调用
        _rightScrollViewer.Offset = new Vector(e.OffsetDelta.X + _rightScrollViewer.Offset.X, e.OffsetDelta.Y + _rightScrollViewer.Offset.Y);
        _isScrollSyncEnabled = true;
    }

    private void OnRightScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (!_isScrollSyncEnabled || _leftScrollViewer == null) return;

        _isScrollSyncEnabled = false; // 防止递归调用
        _leftScrollViewer.Offset = new Vector(e.OffsetDelta.X + _leftScrollViewer.Offset.X, e.OffsetDelta.Y + _leftScrollViewer.Offset.Y);
        _isScrollSyncEnabled = true;
    }

    #endregion

    #region 状态更新

    private void UpdateStatus(string message)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
            statusText.Text = message;
    }

    #endregion
} 
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using SmartToolbox.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

public partial class FileMoverView : UserControl
{
    private readonly FileMoverViewModel _vm;
    private ScrollViewer? _leftScrollViewer;
    private ScrollViewer? _rightScrollViewer;
    private bool _isScrollSyncEnabled = true;

    public FileMoverView()
    {
        InitializeComponent();

        _vm = new FileMoverViewModel();

        // 注入文件夹选择回调
        _vm.BrowseSourceFolder = () => BrowseFolderAsync("选择源文件夹");
        _vm.BrowseTargetFolder = () => BrowseFolderAsync("选择目标文件夹");
        _vm.ConfirmMove = ShowConfirmMoveDialogAsync;

        DataContext = _vm;

        // 初始化滚动同步
        Loaded += (_, _) => InitializeScrollSync();

        // 初始化拖拽
        InitializeDragDrop();

        // 清理
        Unloaded += (_, _) => _vm.Dispose();
    }

    // ── 文件夹选择 ──────────────────────────────────────────────

    private async Task<string?> BrowseFolderAsync(string title)
    {
        if (TopLevel.GetTopLevel(this) is not Window window) return null;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    // ── 确认对话框 ──────────────────────────────────────────────

    private async Task<bool> ShowConfirmMoveDialogAsync(
        string source, string target, int fileCount, long totalSize)
    {
        var sizeText = totalSize switch
        {
            < 1024 => $"{totalSize} B",
            < 1024 * 1024 => $"{totalSize / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{totalSize / (1024.0 * 1024):F1} MB",
            _ => $"{totalSize / (1024.0 * 1024 * 1024):F2} GB"
        };

        var dialog = new Window
        {
            Title = "确认移动",
            Width = 480,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"即将移动 {fileCount} 个文件 ({sizeText})",
                        FontSize = 15,
                        FontWeight = Avalonia.Media.FontWeight.Medium
                    },
                    new TextBlock
                    {
                        Text = $"从: {source}",
                        FontSize = 12,
                        Foreground = Avalonia.Media.Brushes.Gray,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = $"到: {target}",
                        FontSize = 12,
                        Foreground = Avalonia.Media.Brushes.Gray,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 12,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Margin = new Thickness(0, 12, 0, 0),
                        Children =
                        {
                            new Button
                            {
                                Content = "取消",
                                MinWidth = 80,
                                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Classes = { "cancel" }
                            },
                            new Button
                            {
                                Content = "确认移动",
                                MinWidth = 80,
                                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                Classes = { "success" }
                            }
                        }
                    }
                }
            }
        };

        bool result = false;

        var cancelBtn = ((StackPanel)((StackPanel)dialog.Content!).Children[3]).Children[0] as Button;
        var confirmBtn = ((StackPanel)((StackPanel)dialog.Content!).Children[3]).Children[1] as Button;

        if (cancelBtn != null)
            cancelBtn.Click += (_, _) => dialog.Close();
        if (confirmBtn != null)
            confirmBtn.Click += (_, _) => { result = true; dialog.Close(); };

        if (TopLevel.GetTopLevel(this) is Window owner)
            await dialog.ShowDialog(owner);

        return result;
    }

    // ── 拖拽支持 ────────────────────────────────────────────────

    private void InitializeDragDrop()
    {
        var sourceBox = this.FindControl<TextBox>("SourceFolderTextBox");
        var targetBox = this.FindControl<TextBox>("TargetFolderTextBox");

        if (sourceBox != null)
        {
            DragDrop.SetAllowDrop(sourceBox, true);
            sourceBox.AddHandler(DragDrop.DragOverEvent, SourceTargetDragOver);
            sourceBox.AddHandler(DragDrop.DropEvent, SourceDrop);
        }

        if (targetBox != null)
        {
            DragDrop.SetAllowDrop(targetBox, true);
            targetBox.AddHandler(DragDrop.DragOverEvent, SourceTargetDragOver);
            targetBox.AddHandler(DragDrop.DropEvent, TargetDrop);
        }
    }

    private void SourceTargetDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void SourceDrop(object? sender, DragEventArgs e)
    {
        var folder = GetDroppedFolder(e);
        if (folder != null)
        {
            _vm.SourceFolderPath = folder;
            _vm.StatusMessage = $"已选择源文件夹: {folder}";
        }
    }

    private void TargetDrop(object? sender, DragEventArgs e)
    {
        var folder = GetDroppedFolder(e);
        if (folder != null)
        {
            _vm.TargetFolderPath = folder;
            _vm.StatusMessage = $"已选择目标文件夹: {folder}";
        }
    }

    private static string? GetDroppedFolder(DragEventArgs e)
    {
        var files = e.Data.GetFiles();
        if (files is null) return null;

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path != null && System.IO.Directory.Exists(path))
                return path;
        }
        return null;
    }

    // ── 滚动同步 ────────────────────────────────────────────────

    private void InitializeScrollSync()
    {
        try
        {
            _leftScrollViewer = this.FindControl<ScrollViewer>("LeftScrollViewer");
            _rightScrollViewer = this.FindControl<ScrollViewer>("RightScrollViewer");

            if (_leftScrollViewer != null)
                _leftScrollViewer.ScrollChanged += OnLeftScrollChanged;
            if (_rightScrollViewer != null)
                _rightScrollViewer.ScrollChanged += OnRightScrollChanged;
        }
        catch
        {
            _isScrollSyncEnabled = false;
        }
    }

    private void OnLeftScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (!_isScrollSyncEnabled || _rightScrollViewer == null) return;
        try
        {
            _isScrollSyncEnabled = false;
            if (sender is ScrollViewer left)
                _rightScrollViewer.Offset = new Vector(_rightScrollViewer.Offset.X, left.Offset.Y);
        }
        finally { _isScrollSyncEnabled = true; }
    }

    private void OnRightScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (!_isScrollSyncEnabled || _leftScrollViewer == null) return;
        try
        {
            _isScrollSyncEnabled = false;
            if (sender is ScrollViewer right)
                _leftScrollViewer.Offset = new Vector(_leftScrollViewer.Offset.X, right.Offset.Y);
        }
        finally { _isScrollSyncEnabled = true; }
    }
}

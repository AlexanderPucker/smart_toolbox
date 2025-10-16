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
using System.Threading;
using System.Threading.Tasks;

namespace SmartToolbox.Views;

/// <summary>
/// 文件移动视图类
/// 提供文件批量移动、预览、过滤和大小写转换功能的用户控件
/// </summary>
public partial class FileMoverView : UserControl
{
    // 移动文件功能相关字段
    private readonly ObservableCollection<string> _sourceFiles = new(); // 源文件列表
    private readonly ObservableCollection<string> _movePreviewFiles = new(); // 移动预览文件列表
    private readonly List<FileInfo> _filesToMove = new(); // 待移动文件列表
    private string _sourceFolderPath = ""; // 源文件夹路径
    private string _targetFolderPath = ""; // 目标文件夹路径
    
    // 异步处理相关字段
    private CancellationTokenSource? _cancellationTokenSource; // 取消令牌源，用于取消异步操作
    private bool _isProcessing = false; // 是否正在处理中
    private const int MAX_FILES_WARNING = 10000; // 文件数量警告阈值
    private const int BATCH_SIZE = 100; // 批处理大小，用于分批处理大量文件以避免UI卡顿
    
    // 性能优化相关常量
    private const int MAX_PREVIEW_FILES = 5000; // 预览文件数量上限，避免预览过多文件导致性能问题
    
    // 滚动同步相关字段
    private ScrollViewer? _leftScrollViewer; // 左侧滚动视图
    private ScrollViewer? _rightScrollViewer; // 右侧滚动视图
    private bool _isScrollSyncEnabled = true; // 是否启用滚动同步
    private bool _scrollSyncInitialized = false; // 滚动同步是否已初始化

    /// <summary>
    /// 文件移动视图构造函数
    /// 初始化组件和相关控件
    /// </summary>
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
        
        // 初始化大小写转换下拉框
        InitializeCaseConversionComboBox();
        
        // 初始化滚动同步
        InitializeScrollSync();
    }

    #region 移动文件功能

    /// <summary>
    /// 初始化移动文件过滤器下拉框
    /// 设置过滤器选项并默认选择第一项
    /// </summary>
    private void InitializeMoveFileFilterComboBox()
    {
        var moveFileFilterComboBox = this.FindControl<ComboBox>("MoveFileFilterComboBox");
        if (moveFileFilterComboBox != null)
        {
            moveFileFilterComboBox.ItemsSource = FileFilterConfig.CommonFilters;
            moveFileFilterComboBox.SelectedIndex = 0; // 默认选择"所有文件"
        }
    }

    /// <summary>
    /// 初始化大小写转换下拉框
    /// 设置大小写转换选项并默认选择第一项
    /// </summary>
    private void InitializeCaseConversionComboBox()
    {
        var caseConversionComboBox = this.FindControl<ComboBox>("CaseConversionComboBox");
        if (caseConversionComboBox != null)
        {
            caseConversionComboBox.ItemsSource = CaseConversionConfig.ConversionOptions;
            caseConversionComboBox.SelectedIndex = 0; // 默认选择"保持原始大小写"
        }
    }

    /// <summary>
    /// 选择源文件夹事件处理方法
    /// 异步调用选择源文件夹功能
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">路由事件参数</param>
    private async void SelectSourceFolder(object? sender, RoutedEventArgs e)
    {
        await SelectSourceFolderAsync();
    }
    
    /// <summary>
    /// 异步选择源文件夹方法
    /// 使用文件系统选择器让用户选择源文件夹
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task SelectSourceFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        try
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择源文件夹",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                _sourceFolderPath = folders[0].Path.LocalPath;
                var sourceFolderText = this.FindControl<TextBlock>("SourceFolderText");
                if (sourceFolderText != null)
                    sourceFolderText.Text = _sourceFolderPath;
                    
                UpdateStatus($"已选择源文件夹: {_sourceFolderPath}");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"选择源文件夹失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 选择目标文件夹事件处理方法
    /// 异步调用选择目标文件夹功能
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">路由事件参数</param>
    private async void SelectTargetFolder(object? sender, RoutedEventArgs e)
    {
        await SelectTargetFolderAsync();
    }
    
    /// <summary>
    /// 异步选择目标文件夹方法
    /// 使用文件系统选择器让用户选择目标文件夹
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task SelectTargetFolderAsync()
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

    /// <summary>
    /// 预览移动文件事件处理方法
    /// 异步调用预览移动文件功能
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">路由事件参数</param>
    private async void PreviewMoveFiles(object? sender, RoutedEventArgs e)
    {
        await PreviewMoveFilesWrapperAsync();
    }
    
    /// <summary>
    /// 预览移动文件包装方法
    /// 检查前置条件并调用核心预览方法
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task PreviewMoveFilesWrapperAsync()
    {
        if (_isProcessing)
        {
            // 取消当前操作
            _cancellationTokenSource?.Cancel();
            return;
        }

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
            await PreviewMoveFilesAsync();
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("操作已取消");
        }
        catch (Exception ex)
        {
            UpdateStatus($"预览文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步预览移动文件核心方法
    /// 扫描源文件夹中的文件并生成预览列表
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task PreviewMoveFilesAsync()
    {
        _isProcessing = true;
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            // 更新按钮状态
            UpdatePreviewButtonState(true);
            
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

            // 清空现有数据
            _filesToMove.Clear();
            _sourceFiles.Clear();
            _movePreviewFiles.Clear();

            UpdateStatus("正在扫描文件...");

            // 异步扫描文件
            var allFiles = new List<FileInfo>();
            await Task.Run(() =>
            {
                foreach (var pattern in patterns)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var files = Directory.GetFiles(_sourceFolderPath, pattern, searchOption);
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var fileInfo = new FileInfo(file);
                        if (!allFiles.Any(f => f.FullName == fileInfo.FullName))
                        {
                            allFiles.Add(fileInfo);
                        }
                    }
                }
            }, cancellationToken);

            // 检查文件数量
            if (allFiles.Count > MAX_FILES_WARNING)
            {
                UpdateStatus($"警告：找到 {allFiles.Count} 个文件，数量较多可能影响性能");
                await Task.Delay(2000, cancellationToken); // 给用户时间看到警告
                
                // 如果文件数量过多，可以考虑只加载前面的文件
                if (allFiles.Count > MAX_PREVIEW_FILES)
                {
                    UpdateStatus($"文件数量过多 ({allFiles.Count})，为了性能考虑，只显示前 {MAX_PREVIEW_FILES} 个文件");
                    allFiles = allFiles.Take(MAX_PREVIEW_FILES).ToList();
                }
            }
            else if (allFiles.Count > MAX_PREVIEW_FILES)
            {
                UpdateStatus($"文件数量较多 ({allFiles.Count})，为了性能考虑，只显示前 {MAX_PREVIEW_FILES} 个文件");
                allFiles = allFiles.Take(MAX_PREVIEW_FILES).ToList();
            }

            // 按文件路径排序
            allFiles.Sort((x, y) => string.Compare(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase));
            _filesToMove.AddRange(allFiles);

            UpdateStatus($"正在加载文件列表... (共 {allFiles.Count} 个文件)");

            // 分批加载到UI
            for (int i = 0; i < allFiles.Count; i += BATCH_SIZE)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var batch = allFiles.Skip(i).Take(BATCH_SIZE);
                
                // 在UI线程中更新界面
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var caseConversion = GetSelectedCaseConversion();
                    
                    foreach (var file in batch)
                    {
                        // 检查源路径和目标路径是否一致
                        var targetFileName = ConvertFileName(file.Name, caseConversion);
                        var targetFilePath = Path.Combine(_targetFolderPath, targetFileName);
                        
                        // 如果源文件路径和目标文件路径相同，则跳过此文件
                        if (string.Equals(file.FullName, targetFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        
                        // 左侧显示：相对于源文件夹的路径
                        var relativePath = Path.GetRelativePath(_sourceFolderPath, file.FullName);
                        _sourceFiles.Add(relativePath);
                        
                        // 右侧显示：目标文件夹+转换后的文件名
                        var targetDisplay = $"{Path.GetFileName(_targetFolderPath)}\\{targetFileName}";
                        _movePreviewFiles.Add(targetDisplay);
                    }
                });

                // 更新进度
                var progress = (i + BATCH_SIZE) * 100 / allFiles.Count;
                if (progress > 100) progress = 100;
                UpdateStatus($"加载进度: {progress}% ({Math.Min(i + BATCH_SIZE, allFiles.Count)}/{allFiles.Count})");
                
                // 短暂延迟以避免UI卡顿
                await Task.Delay(10, cancellationToken);
            }

            UpdateStatus($"预览完成，找到 {_filesToMove.Count} 个文件待移动");
        }
        finally
        {
            _isProcessing = false;
            UpdatePreviewButtonState(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void UpdatePreviewButtonState(bool isProcessing)
    {
        var previewButton = this.FindControl<Button>("PreviewButton");
        if (previewButton != null)
        {
            // 按钮内容是TextBlock，需要更新TextBlock的Text属性
            if (previewButton.Content is TextBlock textBlock)
            {
                textBlock.Text = isProcessing ? "取消" : "预览移动";
            }
            else
            {
                // 如果不是TextBlock，创建一个新的TextBlock
                previewButton.Content = new TextBlock { Text = isProcessing ? "取消" : "预览移动" };
            }
        }
    }

    private async void ExecuteMoveFiles(object? sender, RoutedEventArgs e)
    {
        await ExecuteMoveFilesWrapperAsync();
    }
    
    private async Task ExecuteMoveFilesWrapperAsync()
    {
        if (_isProcessing)
        {
            // 取消当前操作
            _cancellationTokenSource?.Cancel();
            return;
        }

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

        if (_filesToMove.Count == 0)
        {
            UpdateStatus("没有要移动的文件，请先预览文件");
            return;
        }

        try
        {
            await ExecuteMoveFilesAsync();
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("文件移动操作已取消");
        }
        catch (Exception ex)
        {
            UpdateStatus($"文件移动失败: {ex.Message}");
        }
    }

    private async Task ExecuteMoveFilesAsync()
    {
        _isProcessing = true;
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            // 更新按钮状态
            UpdateMoveButtonState(true);

            // 确保目标文件夹存在
            Directory.CreateDirectory(_targetFolderPath);

            int successCount = 0;
            var errors = new List<string>();
            var totalFiles = _filesToMove.Count;

            UpdateStatus($"开始移动文件... (共 {totalFiles} 个文件)");

            // 分批处理文件移动
            for (int i = 0; i < totalFiles; i += BATCH_SIZE)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = _filesToMove.Skip(i).Take(BATCH_SIZE);
                
                var caseConversion = GetSelectedCaseConversion();
                
                await Task.Run(() =>
                {
                    foreach (var file in batch)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        try
                        {
                            // 应用大小写转换
                            var convertedFileName = ConvertFileName(file.Name, caseConversion);
                            var targetFilePath = Path.Combine(_targetFolderPath, convertedFileName);
                            
                            // 检查源路径和目标路径是否一致，如果一致则跳过
                            if (string.Equals(file.FullName, targetFilePath, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            
                            // 检查目标文件是否已存在
                            if (File.Exists(targetFilePath))
                            {
                                // 生成唯一的文件名
                                var nameWithoutExt = Path.GetFileNameWithoutExtension(convertedFileName);
                                var extension = Path.GetExtension(convertedFileName);
                                int counter = 1;
                                
                                while (File.Exists(targetFilePath))
                                {
                                    var newName = $"{nameWithoutExt}_{counter}{extension}";
                                    targetFilePath = Path.Combine(_targetFolderPath, newName);
                                    counter++;
                                }
                            }

                            // 检查磁盘空间（简单检查）
                            var targetDrive = new DriveInfo(Path.GetPathRoot(targetFilePath)!);
                            if (targetDrive.AvailableFreeSpace < file.Length)
                            {
                                throw new IOException($"目标磁盘空间不足，需要 {file.Length} 字节，可用 {targetDrive.AvailableFreeSpace} 字节");
                            }
                            
                            // 移动文件
                            File.Move(file.FullName, targetFilePath);
                            Interlocked.Increment(ref successCount);
                        }
                        catch (Exception ex)
                        {
                            lock (errors)
                            {
                                errors.Add($"{file.Name}: {ex.Message}");
                            }
                        }
                    }
                }, cancellationToken);

                // 更新进度
                var progress = (i + BATCH_SIZE) * 100 / totalFiles;
                if (progress > 100) progress = 100;
                
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateStatus($"移动进度: {progress}% ({Math.Min(i + BATCH_SIZE, totalFiles)}/{totalFiles})");
                });

                // 短暂延迟以避免过于频繁的UI更新
                await Task.Delay(50, cancellationToken);
            }

            // 更新最终状态
            var statusMessage = $"移动完成: 成功 {successCount} 个";
            if (errors.Count > 0)
                statusMessage += $", 失败 {errors.Count} 个";

            UpdateStatus(statusMessage);

            if (errors.Count > 0)
            {
                // 显示详细错误信息（限制显示前几个错误）
                var errorSummary = string.Join("; ", errors.Take(3));
                if (errors.Count > 3)
                    errorSummary += $"... 等 {errors.Count} 个错误";
                
                UpdateStatus($"部分文件移动失败: {errorSummary}");
            }

            // 清除预览
            if (successCount > 0)
            {
                ClearMovePreviewInternal();
            }
        }
        finally
        {
            _isProcessing = false;
            UpdateMoveButtonState(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void UpdateMoveButtonState(bool isProcessing)
    {
        var executeButton = this.FindControl<Button>("ExecuteButton");
        if (executeButton != null)
        {
            executeButton.IsEnabled = !isProcessing;
        }
    }

    private void ClearMovePreview(object? sender, RoutedEventArgs e)
    {
        ClearMovePreviewInternal();
    }

    private void ClearMovePreviewInternal()
    {
        // 取消正在进行的操作
        if (_isProcessing)
        {
            _cancellationTokenSource?.Cancel();
        }
        
        _filesToMove.Clear();
        _sourceFiles.Clear();
        _movePreviewFiles.Clear();
        
        // 触发垃圾回收以释放内存
        // 注意：在实际生产环境中，应谨慎使用GC.Collect()，这里仅在明确需要释放大量内存时调用
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        UpdateStatus("已清除移动预览");
    }

    #endregion

    #region 大小写转换功能

    /// <summary>
    /// 转换文件名大小写
    /// 根据指定的转换类型将文件名转换为大写、小写或标题格式
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <param name="conversionType">大小写转换类型</param>
    /// <returns>转换后的文件名</returns>
    private string ConvertFileName(string fileName, CaseConversionType conversionType)
    {
        if (string.IsNullOrEmpty(fileName) || conversionType == CaseConversionType.None)
            return fileName;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        string convertedName = conversionType switch
        {
            CaseConversionType.UpperCase => nameWithoutExtension.ToUpperInvariant(),
            CaseConversionType.LowerCase => nameWithoutExtension.ToLowerInvariant(),
            CaseConversionType.TitleCase => ConvertToTitleCase(nameWithoutExtension),
            _ => nameWithoutExtension
        };

        return convertedName + extension;
    }

    /// <summary>
    /// 将输入字符串转换为标题格式
    /// 首字母大写，其余字母小写
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>标题格式的字符串</returns>
    private string ConvertToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(input.ToLower());
    }

    /// <summary>
    /// 获取选中的大小写转换类型
    /// 从下拉框中获取用户选择的转换选项
    /// </summary>
    /// <returns>选中的大小写转换类型</returns>
    private CaseConversionType GetSelectedCaseConversion()
    {
        var caseConversionComboBox = this.FindControl<ComboBox>("CaseConversionComboBox");
        if (caseConversionComboBox?.SelectedItem is CaseConversionItem selectedItem)
        {
            return selectedItem.ConversionType;
        }
        return CaseConversionType.None;
    }

    #endregion

    #region 滚动同步

    /// <summary>
    /// 初始化滚动同步功能
    /// 延迟初始化以确保控件完全加载后绑定滚动事件
    /// </summary>
    private void InitializeScrollSync()
    {
        if (_scrollSyncInitialized) return;
        
        // 延迟初始化滚动同步，确保控件已完全加载
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_scrollSyncInitialized) return;
                
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
                
                _scrollSyncInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化滚动同步失败: {ex.Message}");
                // 如果滚动同步初始化失败，禁用同步功能
                _isScrollSyncEnabled = false;
            }
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// 左侧滚动视图滚动变化事件处理方法
    /// 同步右侧滚动视图的位置
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">滚动变化事件参数</param>
    private void OnLeftScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // 避免在初始化过程中处理滚动事件
        if (!_scrollSyncInitialized) return;
        
        if (!_isScrollSyncEnabled || _rightScrollViewer == null) return;

        try
        {
            _isScrollSyncEnabled = false; // 防止递归调用
            
            // 直接同步滚动位置，而不是累加偏移量
            if (sender is ScrollViewer leftScroll)
            {
                _rightScrollViewer.Offset = new Vector(_rightScrollViewer.Offset.X, leftScroll.Offset.Y);
            }
        }
        catch (Exception ex)
        {
            // 避免滚动同步错误导致崩溃
            // 在生产环境中，可以考虑使用更完善的日志记录机制
            System.Diagnostics.Debug.WriteLine($"滚动同步错误: {ex.Message}");
        }
        finally
        {
            _isScrollSyncEnabled = true;
        }
    }

    /// <summary>
    /// 右侧滚动视图滚动变化事件处理方法
    /// 同步左侧滚动视图的位置
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">滚动变化事件参数</param>
    private void OnRightScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // 避免在初始化过程中处理滚动事件
        if (!_scrollSyncInitialized) return;
        
        if (!_isScrollSyncEnabled || _leftScrollViewer == null) return;

        try
        {
            _isScrollSyncEnabled = false; // 防止递归调用
            
            // 直接同步滚动位置，而不是累加偏移量
            if (sender is ScrollViewer rightScroll)
            {
                _leftScrollViewer.Offset = new Vector(_leftScrollViewer.Offset.X, rightScroll.Offset.Y);
            }
        }
        catch (Exception ex)
        {
            // 避免滚动同步错误导致崩溃
            // 在生产环境中，可以考虑使用更完善的日志记录机制
            System.Diagnostics.Debug.WriteLine($"滚动同步错误: {ex.Message}");
        }
        finally
        {
            _isScrollSyncEnabled = true;
        }
    }

    /// <summary>
    /// 禁用滚动同步功能（如果遇到问题时调用）
    /// </summary>
    public void DisableScrollSync()
    {
        _isScrollSyncEnabled = false;
        
        // 解绑事件
        if (_leftScrollViewer != null)
        {
            _leftScrollViewer.ScrollChanged -= OnLeftScrollChanged;
        }
        if (_rightScrollViewer != null)
        {
            _rightScrollViewer.ScrollChanged -= OnRightScrollChanged;
        }
    }

    #endregion

    #region 资源清理

    /// <summary>
    /// 释放资源方法
    /// 取消所有正在进行的操作并清理事件处理程序
    /// </summary>
    public void Dispose()
    {
        // 取消所有正在进行的操作
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        
        // 清理事件处理程序
        if (_leftScrollViewer != null)
        {
            _leftScrollViewer.ScrollChanged -= OnLeftScrollChanged;
        }
        if (_rightScrollViewer != null)
        {
            _rightScrollViewer.ScrollChanged -= OnRightScrollChanged;
        }
    }

    #endregion

    #region 状态更新

    /// <summary>
    /// 更新状态信息
    /// 在界面上显示指定的状态消息
    /// </summary>
    /// <param name="message">要显示的状态消息</param>
    private void UpdateStatus(string message)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
            statusText.Text = message;
    }

    #endregion
}
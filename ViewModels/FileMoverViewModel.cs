using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartToolbox.ViewModels;

public partial class FileMoverViewModel : ViewModelBase, IDisposable
{
    // ── 可绑定属性 ──────────────────────────────────────────────

    [ObservableProperty]
    private string _sourceFolderPath = "";

    [ObservableProperty]
    private string _targetFolderPath = "";

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _includeSubfolders = true;

    [ObservableProperty]
    private FileFilterItem? _selectedFilter;

    [ObservableProperty]
    private CaseConversionItem? _selectedCaseConversion;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private string _totalSizeText = "";

    // ── 集合 ───────────────────────────────────────────────────

    public ObservableCollection<FileMoveInfo> PreviewItems { get; } = new();

    public ObservableCollection<FileFilterItem> FilterOptions { get; } =
        new(FileFilterConfig.CommonFilters);

    public ObservableCollection<CaseConversionItem> CaseConversionOptions { get; } =
        new(CaseConversionConfig.ConversionOptions);

    // ── 内部状态 ────────────────────────────────────────────────

    private readonly List<FileInfo> _filesToMove = new();
    private CancellationTokenSource? _cts;
    private const int BatchSize = 100;
    private const int MaxFilesWarning = 10000;

    // 文件夹选择回调（View 层注入）
    public Func<Task<string?>>? BrowseSourceFolder { get; set; }
    public Func<Task<string?>>? BrowseTargetFolder { get; set; }
    public Func<string, string, int, long, Task<bool>>? ConfirmMove { get; set; }

    // ── 初始化 ──────────────────────────────────────────────────

    public FileMoverViewModel()
    {
        SelectedFilter = FilterOptions.FirstOrDefault();
        SelectedCaseConversion = CaseConversionOptions.FirstOrDefault();
    }

    // ── 文件夹选择命令 ──────────────────────────────────────────

    [RelayCommand]
    private async Task SelectSourceFolderAsync()
    {
        if (BrowseSourceFolder is null) return;
        var path = await BrowseSourceFolder();
        if (!string.IsNullOrEmpty(path))
        {
            SourceFolderPath = path;
            StatusMessage = $"已选择源文件夹: {path}";
        }
    }

    [RelayCommand]
    private async Task SelectTargetFolderAsync()
    {
        if (BrowseTargetFolder is null) return;
        var path = await BrowseTargetFolder();
        if (!string.IsNullOrEmpty(path))
        {
            TargetFolderPath = path;
            StatusMessage = $"已选择目标文件夹: {path}";
        }
    }

    // ── 预览命令 ────────────────────────────────────────────────

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (IsProcessing)
        {
            _cts?.Cancel();
            return;
        }

        if (!ValidateFolders()) return;

        IsProcessing = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        ProgressValue = 0;

        try
        {
            await ScanAndBuildPreviewAsync(ct);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "操作已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"预览失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task ScanAndBuildPreviewAsync(CancellationToken ct)
    {
        var patterns = (SelectedFilter?.Pattern ?? "*.*")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();

        var searchOption = IncludeSubfolders
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        _filesToMove.Clear();
        PreviewItems.Clear();

        StatusMessage = "正在扫描文件...";

        // 扫描文件
        var allFiles = new List<FileInfo>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await Task.Run(() =>
        {
            foreach (var pattern in patterns)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var file in Directory.GetFiles(SourceFolderPath, pattern, searchOption))
                {
                    ct.ThrowIfCancellationRequested();
                    if (seenPaths.Add(file))
                        allFiles.Add(new FileInfo(file));
                }
            }
        }, ct);

        if (allFiles.Count > MaxFilesWarning)
        {
            StatusMessage = $"警告：找到 {allFiles.Count} 个文件，数量较多";
            await Task.Delay(2000, ct);
            if (allFiles.Count > MaxFilesWarning * 2)
            {
                StatusMessage = $"文件过多 ({allFiles.Count})，仅显示前 {MaxFilesWarning} 个";
                allFiles = allFiles.Take(MaxFilesWarning).ToList();
            }
        }

        allFiles.Sort((a, b) =>
            string.Compare(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase));
        _filesToMove.AddRange(allFiles);

        // 计算总大小
        long totalSize = allFiles.Sum(f => f.Length);
        TotalSizeText = FormatSize(totalSize);
        FileCount = allFiles.Count;

        StatusMessage = $"正在加载预览... (共 {allFiles.Count} 个文件)";

        // 分批加载到 UI
        var caseConversion = SelectedCaseConversion?.ConversionType ?? CaseConversionType.None;

        for (int i = 0; i < allFiles.Count; i += BatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = allFiles.Skip(i).Take(BatchSize);
            foreach (var file in batch)
            {
                var relativePath = Path.GetRelativePath(SourceFolderPath, file.FullName);
                var targetFileName = ConvertFileName(file.Name, caseConversion);
                var targetDisplay = $"{Path.GetFileName(TargetFolderPath)}{Path.DirectorySeparatorChar}{targetFileName}";

                PreviewItems.Add(new FileMoveInfo
                {
                    SourceRelativePath = relativePath,
                    TargetDisplayName = targetDisplay,
                    FileSize = file.Length
                });
            }

            var progress = Math.Min((i + BatchSize) * 100.0 / allFiles.Count, 100);
            ProgressValue = progress;
            StatusMessage = $"加载进度: {progress:F0}% ({Math.Min(i + BatchSize, allFiles.Count)}/{allFiles.Count})";

            await Task.Delay(10, ct);
        }

        StatusMessage = $"预览完成，共 {_filesToMove.Count} 个文件 ({TotalSizeText})";
    }

    // ── 执行移动命令 ────────────────────────────────────────────

    [RelayCommand]
    private async Task ExecuteMoveAsync()
    {
        if (IsProcessing)
        {
            StatusMessage = "正在处理中，请稍候...";
            return;
        }

        if (_filesToMove.Count == 0)
        {
            StatusMessage = "没有要移动的文件，请先预览";
            return;
        }

        if (!ValidateFolders()) return;

        // 确认对话框
        if (ConfirmMove is not null)
        {
            var confirmed = await ConfirmMove(
                SourceFolderPath, TargetFolderPath,
                _filesToMove.Count, _filesToMove.Sum(f => f.Length));
            if (!confirmed) return;
        }

        IsProcessing = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        ProgressValue = 0;

        try
        {
            await ExecuteMoveInternalAsync(ct);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "移动操作已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"移动失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task ExecuteMoveInternalAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(TargetFolderPath);

        int successCount = 0;
        var errors = new List<string>();
        var totalFiles = _filesToMove.Count;
        var caseConversion = SelectedCaseConversion?.ConversionType ?? CaseConversionType.None;

        StatusMessage = $"开始移动文件... (共 {totalFiles} 个文件)";

        for (int i = 0; i < totalFiles; i += BatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = _filesToMove.Skip(i).Take(BatchSize).ToList();

            await Task.Run(() =>
            {
                foreach (var file in batch)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var convertedName = ConvertFileName(file.Name, caseConversion);
                        var targetPath = Path.Combine(TargetFolderPath, convertedName);

                        if (File.Exists(targetPath))
                        {
                            var nameNoExt = Path.GetFileNameWithoutExtension(convertedName);
                            var ext = Path.GetExtension(convertedName);
                            int counter = 1;
                            while (File.Exists(targetPath))
                            {
                                targetPath = Path.Combine(TargetFolderPath, $"{nameNoExt}_{counter}{ext}");
                                counter++;
                            }
                        }

                        var drive = new DriveInfo(Path.GetPathRoot(targetPath)!);
                        if (drive.AvailableFreeSpace < file.Length)
                            throw new IOException($"磁盘空间不足");

                        File.Move(file.FullName, targetPath);
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Exception ex)
                    {
                        lock (errors) { errors.Add($"{file.Name}: {ex.Message}"); }
                    }
                }
            }, ct);

            var progress = Math.Min((i + BatchSize) * 100.0 / totalFiles, 100);
            ProgressValue = progress;
            StatusMessage = $"移动进度: {progress:F0}% ({Math.Min(i + BatchSize, totalFiles)}/{totalFiles})";
            await Task.Delay(50, ct);
        }

        var msg = $"移动完成: 成功 {successCount} 个";
        if (errors.Count > 0)
        {
            msg += $", 失败 {errors.Count} 个";
            var detail = string.Join("; ", errors.Take(3));
            if (errors.Count > 3) detail += $"... 等 {errors.Count} 个错误";
            StatusMessage = $"{msg} | {detail}";
        }
        else
        {
            StatusMessage = msg;
        }

        if (successCount > 0) ClearPreviewInternal();
    }

    // ── 清除预览命令 ────────────────────────────────────────────

    [RelayCommand]
    private void ClearPreview()
    {
        ClearPreviewInternal();
    }

    private void ClearPreviewInternal()
    {
        if (IsProcessing) _cts?.Cancel();
        _filesToMove.Clear();
        PreviewItems.Clear();
        FileCount = 0;
        TotalSizeText = "";
        ProgressValue = 0;
        StatusMessage = "已清除预览";
    }

    // ── 辅助方法 ────────────────────────────────────────────────

    private bool ValidateFolders()
    {
        if (string.IsNullOrEmpty(SourceFolderPath) || !Directory.Exists(SourceFolderPath))
        {
            StatusMessage = "请选择有效的源文件夹";
            return false;
        }
        if (string.IsNullOrEmpty(TargetFolderPath) || !Directory.Exists(TargetFolderPath))
        {
            StatusMessage = "请选择有效的目标文件夹";
            return false;
        }
        return true;
    }

    private static string ConvertFileName(string fileName, CaseConversionType type)
    {
        if (string.IsNullOrEmpty(fileName) || type == CaseConversionType.None)
            return fileName;

        var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        var converted = type switch
        {
            CaseConversionType.UpperCase => nameNoExt.ToUpperInvariant(),
            CaseConversionType.LowerCase => nameNoExt.ToLowerInvariant(),
            CaseConversionType.TitleCase => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nameNoExt.ToLower()),
            _ => nameNoExt
        };

        return converted + ext;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    // ── 资源清理 ────────────────────────────────────────────────

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

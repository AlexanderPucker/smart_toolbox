using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SmartToolbox.ViewModels;

public partial class HashCalculatorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private string _md5Hash = "";

    [ObservableProperty]
    private string _sha1Hash = "";

    [ObservableProperty]
    private string _sha256Hash = "";

    [ObservableProperty]
    private string _sha512Hash = "";

    [ObservableProperty]
    private string _statusMessage = "输入文本或选择文件计算哈希值";

    [ObservableProperty]
    private bool _isUpperCase = true;

    [ObservableProperty]
    private string _filePath = "";

    // 文件夹选择回调
    public Func<Task<string?>>? BrowseFile { get; set; }

    [RelayCommand]
    private void CalculateTextHash()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            StatusMessage = "请输入要计算哈希的文本";
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(InputText);
        ComputeHashes(bytes);
        StatusMessage = $"文本哈希计算完成 ({InputText.Length} 字符)";
    }

    [RelayCommand]
    private async Task CalculateFileHashAsync()
    {
        if (BrowseFile is null) return;
        var path = await BrowseFile();
        if (string.IsNullOrEmpty(path)) return;

        FilePath = path;
        StatusMessage = "正在计算文件哈希...";

        try
        {
            var bytes = await File.ReadAllBytesAsync(path);
            ComputeHashes(bytes);
            var fi = new FileInfo(path);
            StatusMessage = $"文件哈希计算完成: {fi.Name} ({FormatSize(fi.Length)})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"读取文件失败: {ex.Message}";
        }
    }

    private void ComputeHashes(byte[] data)
    {
        Md5Hash = FormatHash(MD5.HashData(data));
        Sha1Hash = FormatHash(SHA1.HashData(data));
        Sha256Hash = FormatHash(SHA256.HashData(data));
        Sha512Hash = FormatHash(SHA512.HashData(data));
    }

    private string FormatHash(byte[] hash)
    {
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString(IsUpperCase ? "X2" : "x2"));
        return sb.ToString();
    }

    partial void OnIsUpperCaseChanged(bool value)
    {
        // 重新格式化已有哈希值
        if (!string.IsNullOrEmpty(Md5Hash))
        {
            Md5Hash = ConvertCase(Md5Hash, value);
            Sha1Hash = ConvertCase(Sha1Hash, value);
            Sha256Hash = ConvertCase(Sha256Hash, value);
            Sha512Hash = ConvertCase(Sha512Hash, value);
        }
    }

    private static string ConvertCase(string hex, bool upper)
    {
        return upper ? hex.ToUpperInvariant() : hex.ToLowerInvariant();
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = "";
        FilePath = "";
        Md5Hash = "";
        Sha1Hash = "";
        Sha256Hash = "";
        Sha512Hash = "";
        StatusMessage = "已清空";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

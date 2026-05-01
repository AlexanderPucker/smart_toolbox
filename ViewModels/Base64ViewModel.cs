using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;

namespace SmartToolbox.ViewModels;

public partial class Base64ViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private string _outputText = "";

    [ObservableProperty]
    private string _statusMessage = "输入文本进行 Base64 编码或解码";

    [ObservableProperty]
    private bool _isUrlSafe;

    public Func<string, System.Threading.Tasks.Task>? CopyToClipboard { get; set; }

    [RelayCommand]
    private void Encode()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            StatusMessage = "请输入要编码的文本";
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(InputText);
            var result = Convert.ToBase64String(bytes);
            if (IsUrlSafe)
                result = result.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            OutputText = result;
            StatusMessage = $"编码成功 ({InputText.Length} 字符 → {result.Length} 字符)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"编码失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Decode()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            StatusMessage = "请输入要解码的 Base64 文本";
            return;
        }

        try
        {
            var base64 = InputText.Trim();
            if (IsUrlSafe)
                base64 = base64.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var bytes = Convert.FromBase64String(base64);
            OutputText = Encoding.UTF8.GetString(bytes);
            StatusMessage = $"解码成功 ({InputText.Length} 字符 → {OutputText.Length} 字符)";
        }
        catch (FormatException)
        {
            StatusMessage = "无效的 Base64 文本";
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CopyOutputAsync()
    {
        if (string.IsNullOrEmpty(OutputText)) return;
        if (CopyToClipboard is not null)
        {
            await CopyToClipboard(OutputText);
            StatusMessage = "已复制到剪贴板";
        }
    }

    [RelayCommand]
    private void Swap()
    {
        (InputText, OutputText) = (OutputText, InputText);
        StatusMessage = "已交换输入输出";
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = "";
        OutputText = "";
        StatusMessage = "已清空";
    }
}

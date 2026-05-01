using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.Json;

namespace SmartToolbox.ViewModels;

public partial class JsonFormatterViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputJson = "";

    [ObservableProperty]
    private string _outputJson = "";

    [ObservableProperty]
    private string _statusMessage = "粘贴 JSON 文本，然后点击格式化";

    [ObservableProperty]
    private bool _isCompact;

    public Func<string, System.Threading.Tasks.Task>? CopyToClipboard { get; set; }

    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(InputJson))
        {
            StatusMessage = "请输入 JSON 文本";
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(InputJson);
            var options = new JsonSerializerOptions
            {
                WriteIndented = !IsCompact,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            OutputJson = JsonSerializer.Serialize(doc.RootElement, options);
            StatusMessage = $"格式化成功 ({OutputJson.Length} 字符)";
        }
        catch (JsonException ex)
        {
            OutputJson = "";
            StatusMessage = $"JSON 格式错误: {ex.Message}";
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task CopyOutputAsync()
    {
        if (string.IsNullOrEmpty(OutputJson))
        {
            StatusMessage = "没有可复制的内容";
            return;
        }

        if (CopyToClipboard is not null)
        {
            await CopyToClipboard(OutputJson);
            StatusMessage = "已复制到剪贴板";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputJson = "";
        OutputJson = "";
        StatusMessage = "已清空";
    }

    [RelayCommand]
    private void Swap()
    {
        (InputJson, OutputJson) = (OutputJson, InputJson);
        StatusMessage = "已交换输入输出";
    }
}

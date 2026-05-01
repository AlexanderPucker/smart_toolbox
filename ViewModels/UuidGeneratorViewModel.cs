using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;

namespace SmartToolbox.ViewModels;

public partial class UuidGeneratorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _outputText = "";

    [ObservableProperty]
    private string _statusMessage = "生成 UUID / GUID";

    [ObservableProperty]
    private bool _isUpperCase;

    [ObservableProperty]
    private bool _withBraces;

    [ObservableProperty]
    private bool _withHyphens = true;

    [ObservableProperty]
    private int _generateCount = 1;

    public Func<string, System.Threading.Tasks.Task>? CopyToClipboard { get; set; }

    [RelayCommand]
    private void Generate()
    {
        var count = Math.Clamp(GenerateCount, 1, 1000);
        var sb = new StringBuilder();

        for (int i = 0; i < count; i++)
        {
            var guid = Guid.NewGuid().ToString("D");
            if (!WithHyphens)
                guid = guid.Replace("-", "");
            if (IsUpperCase)
                guid = guid.ToUpperInvariant();
            if (WithBraces)
                guid = $"{{{guid}}}";

            sb.AppendLine(guid);
        }

        OutputText = sb.ToString().TrimEnd();
        StatusMessage = $"已生成 {count} 个 UUID";
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
    private void Clear()
    {
        OutputText = "";
        StatusMessage = "已清空";
    }
}

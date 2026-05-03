using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartToolbox.Services;

namespace SmartToolbox.ViewModels;

public partial class VoiceViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isSpeaking;

    [ObservableProperty]
    private string _textInput = string.Empty;

    [ObservableProperty]
    private string _transcribedText = string.Empty;

    [ObservableProperty]
    private string _selectedVoice = "alloy";

    [ObservableProperty]
    private VoiceProvider _selectedProvider = VoiceProvider.OpenAI;

    [ObservableProperty]
    private double _speed = 1.0;

    [ObservableProperty]
    private double _pitch = 1.0;

    public ObservableCollection<string> AvailableVoices { get; } = new();
    public ObservableCollection<VoiceHistoryItem> History { get; } = new();

    private readonly VoiceService _voiceService;

    public VoiceViewModel()
    {
        _voiceService = VoiceService.Instance;
        _voiceService.OnTranscriptionComplete += OnTranscriptionComplete;
        _voiceService.OnSynthesisComplete += OnSynthesisComplete;
        _voiceService.OnRecordingStarted += () => IsRecording = true;
        _voiceService.OnRecordingStopped += () => IsRecording = false;
        _voiceService.OnSpeakingStarted += () => IsSpeaking = true;
        _voiceService.OnSpeakingStopped += () => IsSpeaking = false;
        _voiceService.OnError += OnError;

        LoadAvailableVoices();
    }

    private void OnTranscriptionComplete(TranscriptionResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (result.Success)
            {
                TranscribedText = result.Text;
                StatusMessage = $"转录完成 ({result.Duration.TotalSeconds:F1}s)";

                History.Insert(0, new VoiceHistoryItem
                {
                    Type = "语音输入",
                    Content = result.Text,
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                StatusMessage = $"转录失败: {result.ErrorMessage}";
            }
        });
    }

    private void OnSynthesisComplete(SynthesisResult result)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (result.Success)
            {
                StatusMessage = $"合成完成 ({result.Duration.TotalSeconds:F1}s)";
            }
            else
            {
                StatusMessage = $"合成失败: {result.ErrorMessage}";
            }
        });
    }

    private void OnError(string error)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = error;
        });
    }

    partial void OnSelectedProviderChanged(VoiceProvider value)
    {
        _voiceService.Configure(new VoiceSettings { Provider = value });
        LoadAvailableVoices();
    }

    private void LoadAvailableVoices()
    {
        AvailableVoices.Clear();
        foreach (var voice in _voiceService.GetAvailableVoices())
        {
            AvailableVoices.Add(voice);
        }

        if (AvailableVoices.Count > 0)
        {
            SelectedVoice = AvailableVoices[0];
        }
    }

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        if (IsRecording) return;

        StatusMessage = "正在录音...";
        await _voiceService.StartRecordingAsync();
    }

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        if (!IsRecording) return;

        StatusMessage = "正在处理...";
        await _voiceService.StopRecordingAsync();
    }

    [RelayCommand]
    private void CancelRecording()
    {
        _voiceService.CancelRecording();
        StatusMessage = "录音已取消";
    }

    [RelayCommand]
    private async Task SpeakAsync()
    {
        if (string.IsNullOrWhiteSpace(TextInput))
        {
            StatusMessage = "请输入文本";
            return;
        }

        if (IsSpeaking)
        {
            _voiceService.StopSpeaking();
            return;
        }

        StatusMessage = "正在合成语音...";

        _voiceService.Configure(new VoiceSettings
        {
            Provider = SelectedProvider,
            VoiceName = SelectedVoice,
            Speed = Speed,
            Pitch = Pitch
        });

        await _voiceService.SpeakAsync(TextInput, SelectedVoice);

        History.Insert(0, new VoiceHistoryItem
        {
            Type = "语音输出",
            Content = TextInput,
            Timestamp = DateTime.Now
        });
    }

    [RelayCommand]
    private void StopSpeaking()
    {
        _voiceService.StopSpeaking();
        StatusMessage = "已停止播放";
    }

    [RelayCommand]
    private void UseTranscribedText()
    {
        if (!string.IsNullOrWhiteSpace(TranscribedText))
        {
            TextInput = TranscribedText;
            StatusMessage = "已使用转录文本";
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        History.Clear();
        StatusMessage = "历史已清空";
    }

    [RelayCommand]
    private void ClearInput()
    {
        TextInput = string.Empty;
        TranscribedText = string.Empty;
        StatusMessage = "已清空";
    }
}

public class VoiceHistoryItem
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

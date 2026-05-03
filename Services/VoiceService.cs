using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public enum VoiceProvider
{
    System,
    OpenAI,
    Azure,
    ElevenLabs,
    Local
}

public class VoiceSettings
{
    public VoiceProvider Provider { get; set; } = VoiceProvider.System;
    public string VoiceName { get; set; } = string.Empty;
    public double Speed { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;
    public double Volume { get; set; } = 1.0;
    public string Language { get; set; } = "zh-CN";
    public bool AutoDetectLanguage { get; set; } = true;
}

public class TranscriptionResult
{
    public bool Success { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Language { get; set; }
    public double Confidence { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SynthesisResult
{
    public bool Success { get; set; }
    public byte[]? AudioData { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class VoiceService : IDisposable
{
    private static readonly Lazy<VoiceService> _instance = new(() => new VoiceService());
    public static VoiceService Instance => _instance.Value;

    private readonly HttpClient _httpClient;
    private VoiceSettings _settings;
    private bool _isRecording;
    private bool _isSpeaking;
    private CancellationTokenSource? _recordingCts;
    private CancellationTokenSource? _speakingCts;
    private readonly string _audioCachePath;

    public event Action<byte[]>? OnAudioDataReceived;
    public event Action<TranscriptionResult>? OnTranscriptionComplete;
    public event Action<SynthesisResult>? OnSynthesisComplete;
    public event Action? OnRecordingStarted;
    public event Action? OnRecordingStopped;
    public event Action? OnSpeakingStarted;
    public event Action? OnSpeakingStopped;
    public event Action<string>? OnError;

    public bool IsRecording => _isRecording;
    public bool IsSpeaking => _isSpeaking;
    public VoiceSettings Settings => _settings;

    private VoiceService()
    {
        _httpClient = new HttpClient();
        _settings = new VoiceSettings();
        _audioCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "voice_cache");
        Directory.CreateDirectory(_audioCachePath);
    }

    public void Configure(VoiceSettings settings)
    {
        _settings = settings;
    }

    public void Configure(string apiKey, VoiceProvider provider)
    {
        _settings.Provider = provider;
        _httpClient.DefaultRequestHeaders.Clear();

        if (provider == VoiceProvider.OpenAI)
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        else if (provider == VoiceProvider.Azure)
        {
        }
        else if (provider == VoiceProvider.ElevenLabs)
        {
            _httpClient.DefaultRequestHeaders.Add("xi-api-key", apiKey);
        }
    }

    public async Task StartRecordingAsync()
    {
        if (_isRecording) return;

        _isRecording = true;
        _recordingCts = new CancellationTokenSource();
        OnRecordingStarted?.Invoke();

        try
        {
            await Task.Run(async () =>
            {
                while (_isRecording && !_recordingCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, _recordingCts.Token);
                }
            }, _recordingCts.Token);
        }
        catch (OperationCanceledException) { }
    }

    public async Task<TranscriptionResult> StopRecordingAsync()
    {
        if (!_isRecording) return new TranscriptionResult { Success = false, ErrorMessage = "未在录音" };

        _isRecording = false;
        _recordingCts?.Cancel();
        OnRecordingStopped?.Invoke();

        var audioData = Array.Empty<byte>();
        return await TranscribeAsync(audioData);
    }

    public void CancelRecording()
    {
        if (_isRecording)
        {
            _isRecording = false;
            _recordingCts?.Cancel();
            OnRecordingStopped?.Invoke();
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(byte[] audioData, string? language = null)
    {
        var result = new TranscriptionResult();
        var startTime = DateTime.Now;

        try
        {
            if (_settings.Provider == VoiceProvider.OpenAI)
            {
                result = await TranscribeWithOpenAIAsync(audioData, language);
            }
            else if (_settings.Provider == VoiceProvider.Azure)
            {
                result = await TranscribeWithAzureAsync(audioData, language);
            }
            else
            {
                result = new TranscriptionResult
                {
                    Success = false,
                    ErrorMessage = "请配置语音服务提供商"
                };
            }

            result.Duration = DateTime.Now - startTime;
            OnTranscriptionComplete?.Invoke(result);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            OnError?.Invoke($"转录失败: {ex.Message}");
        }

        return result;
    }

    private async Task<TranscriptionResult> TranscribeWithOpenAIAsync(byte[] audioData, string? language)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var audioStream = new MemoryStream(audioData);
            using var audioContent = new StreamContent(audioStream);

            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "file", "audio.wav");
            content.Add(new StringContent("whisper-1"), "model");

            if (!string.IsNullOrEmpty(language))
            {
                content.Add(new StringContent(language), "language");
            }

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<OpenAITranscriptionResponse>(responseContent);
                return new TranscriptionResult
                {
                    Success = true,
                    Text = result?.Text ?? string.Empty,
                    Language = result?.Language,
                    Confidence = 1.0
                };
            }
            else
            {
                return new TranscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"OpenAI API 错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            return new TranscriptionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<TranscriptionResult> TranscribeWithAzureAsync(byte[] audioData, string? language)
    {
        return new TranscriptionResult
        {
            Success = false,
            ErrorMessage = "Azure 语音服务需要配置 SDK"
        };
    }

    public async Task<SynthesisResult> SynthesizeAsync(string text, string? voiceName = null)
    {
        var result = new SynthesisResult();
        var startTime = DateTime.Now;

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = "文本不能为空"
                };
            }

            if (_settings.Provider == VoiceProvider.OpenAI)
            {
                result = await SynthesizeWithOpenAIAsync(text, voiceName);
            }
            else if (_settings.Provider == VoiceProvider.ElevenLabs)
            {
                result = await SynthesizeWithElevenLabsAsync(text, voiceName);
            }
            else
            {
                result = await SynthesizeWithSystemAsync(text);
            }

            result.Duration = DateTime.Now - startTime;
            OnSynthesisComplete?.Invoke(result);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            OnError?.Invoke($"语音合成失败: {ex.Message}");
        }

        return result;
    }

    private async Task<SynthesisResult> SynthesizeWithOpenAIAsync(string text, string? voiceName)
    {
        try
        {
            var voice = voiceName ?? "alloy";
            var request = new { model = "tts-1", input = text, voice };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/speech", content);

            if (response.IsSuccessStatusCode)
            {
                var audioData = await response.Content.ReadAsByteArrayAsync();
                return new SynthesisResult
                {
                    Success = true,
                    AudioData = audioData
                };
            }
            else
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = $"OpenAI TTS 错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<SynthesisResult> SynthesizeWithElevenLabsAsync(string text, string? voiceName)
    {
        try
        {
            var voiceId = voiceName ?? "21m00Tcm4TlvDq8ikWAM";

            var request = new
            {
                text,
                model_id = "eleven_monolingual_v1",
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}",
                content);

            if (response.IsSuccessStatusCode)
            {
                var audioData = await response.Content.ReadAsByteArrayAsync();
                return new SynthesisResult
                {
                    Success = true,
                    AudioData = audioData
                };
            }
            else
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = $"ElevenLabs 错误: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<SynthesisResult> SynthesizeWithSystemAsync(string text)
    {
        return new SynthesisResult
        {
            Success = false,
            ErrorMessage = "系统语音合成需要平台特定实现"
        };
    }

    public async Task SpeakAsync(string text, string? voiceName = null)
    {
        if (_isSpeaking) return;

        _isSpeaking = true;
        _speakingCts = new CancellationTokenSource();
        OnSpeakingStarted?.Invoke();

        try
        {
            var result = await SynthesizeAsync(text, voiceName);

            if (result.Success && result.AudioData != null)
            {
                await PlayAudioAsync(result.AudioData, _speakingCts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            OnError?.Invoke($"播放失败: {ex.Message}");
        }
        finally
        {
            _isSpeaking = false;
            OnSpeakingStopped?.Invoke();
        }
    }

    public void StopSpeaking()
    {
        if (_isSpeaking)
        {
            _speakingCts?.Cancel();
            _isSpeaking = false;
            OnSpeakingStopped?.Invoke();
        }
    }

    private async Task PlayAudioAsync(byte[] audioData, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
    }

    public async Task SaveAudioAsync(byte[] audioData, string filePath)
    {
        await File.WriteAllBytesAsync(filePath, audioData);
    }

    public string GetCachedAudioPath(string textHash)
    {
        return Path.Combine(_audioCachePath, $"{textHash}.mp3");
    }

    public void ClearAudioCache()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_audioCachePath))
            {
                File.Delete(file);
            }
        }
        catch { }
    }

    public List<string> GetAvailableVoices()
    {
        return _settings.Provider switch
        {
            VoiceProvider.OpenAI => new List<string> { "alloy", "echo", "fable", "onyx", "nova", "shimmer" },
            VoiceProvider.Azure => new List<string> { "zh-CN-XiaoxiaoNeural", "zh-CN-YunxiNeural", "en-US-JennyNeural" },
            VoiceProvider.ElevenLabs => new List<string> { "Rachel", "Domi", "Bella", "Antoni", "Adam" },
            _ => new List<string>()
        };
    }

    public void Dispose()
    {
        _recordingCts?.Dispose();
        _speakingCts?.Dispose();
        _httpClient.Dispose();
    }
}

internal class OpenAITranscriptionResponse
{
    public string? Text { get; set; }
    public string? Language { get; set; }
    public double? Duration { get; set; }
}

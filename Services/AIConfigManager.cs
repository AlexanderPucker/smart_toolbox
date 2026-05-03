using System.IO;
using System.Text.Json;

namespace SmartToolbox.Services;

public class AIConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartToolbox",
        "ai_config.json");

    private static AIConfig? _cachedConfig;

    public static AIConfig LoadConfig()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        if (!File.Exists(ConfigPath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AIConfig>(json);
            _cachedConfig = config ?? CreateDefaultConfig();
            return _cachedConfig;
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    public static void SaveConfig(AIConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
            _cachedConfig = config;
        }
        catch
        {
        }
    }

    private static AIConfig CreateDefaultConfig()
    {
        _cachedConfig = new AIConfig
        {
            Provider = AIProvider.OpenAI,
            ApiKey = "",
            ApiUrl = "https://api.openai.com/v1/chat/completions",
            Model = "gpt-3.5-turbo",
            Temperature = 0.7,
            MaxTokens = 2000
        };
        return _cachedConfig;
    }
}

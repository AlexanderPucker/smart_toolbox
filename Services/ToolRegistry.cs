using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolDefinition GetDefinition();
    Task<string> ExecuteAsync(string arguments);
}

public sealed class ToolRegistry
{
    private static readonly Lazy<ToolRegistry> _instance = new(() => new ToolRegistry());
    public static ToolRegistry Instance => _instance.Value;

    private readonly Dictionary<string, ITool> _tools = new();

    public event Action<ITool>? OnToolRegistered;
    public event Action<string, string, string>? OnToolExecuted;

    private ToolRegistry()
    {
        RegisterBuiltInTools();
    }

    private void RegisterBuiltInTools()
    {
        RegisterTool(new CalculateHashTool());
        RegisterTool(new FormatJsonTool());
        RegisterTool(new Base64EncodeTool());
        RegisterTool(new Base64DecodeTool());
        RegisterTool(new GenerateUuidTool());
        RegisterTool(new ConvertTimestampTool());
        RegisterTool(new CalculateExpressionTool());
        RegisterTool(new SearchWebTool());
        RegisterTool(new ReadFileTool());
        RegisterTool(new WriteFileTool());
    }

    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
        OnToolRegistered?.Invoke(tool);
    }

    public void UnregisterTool(string name)
    {
        _tools.Remove(name);
    }

    public ITool? GetTool(string name)
    {
        return _tools.GetValueOrDefault(name);
    }

    public List<ITool> GetAllTools()
    {
        return new List<ITool>(_tools.Values);
    }

    public List<ToolDefinition> GetAllToolDefinitions()
    {
        var definitions = new List<ToolDefinition>();
        foreach (var tool in _tools.Values)
        {
            definitions.Add(tool.GetDefinition());
        }
        return definitions;
    }

    public async Task<string> ExecuteToolAsync(string name, string arguments)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            return $"错误: 未找到工具 '{name}'";
        }

        try
        {
            var result = await tool.ExecuteAsync(arguments);
            OnToolExecuted?.Invoke(name, arguments, result);
            return result;
        }
        catch (Exception ex)
        {
            return $"工具执行错误: {ex.Message}";
        }
    }

    public bool HasTool(string name)
    {
        return _tools.ContainsKey(name);
    }
}

public class CalculateHashTool : ITool
{
    public string Name => "calculate_hash";
    public string Description => "计算文本或文件的哈希值（支持MD5、SHA1、SHA256等算法）";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "要计算哈希的文本内容" },
                        algorithm = new { type = "string", @enum = new[] { "MD5", "SHA1", "SHA256", "SHA512" }, description = "哈希算法" }
                    },
                    required = new[] { "text" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var text = args?["text"].GetString() ?? "";
            var algorithm = args?.TryGetValue("algorithm", out var alg) == true ? alg.GetString() : "SHA256";

            using var hashAlgorithm = algorithm?.ToUpper() switch
            {
                "MD5" => System.Security.Cryptography.MD5.Create(),
                "SHA1" => System.Security.Cryptography.SHA1.Create(),
                "SHA512" => System.Security.Cryptography.SHA512.Create(),
                _ => System.Security.Cryptography.SHA256.Create()
            };

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var hash = hashAlgorithm.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class FormatJsonTool : ITool
{
    public string Name => "format_json";
    public string Description => "格式化JSON字符串，使其更易读";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        json = new { type = "string", description = "要格式化的JSON字符串" },
                        indent = new { type = "integer", description = "缩进空格数，默认2" }
                    },
                    required = new[] { "json" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var json = args?["json"].GetString() ?? "";
            var indent = args?.TryGetValue("indent", out var ind) == true ? ind.GetInt32() : 2;

            var doc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(doc.RootElement, options);
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class Base64EncodeTool : ITool
{
    public string Name => "base64_encode";
    public string Description => "将文本编码为Base64格式";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "要编码的文本" }
                    },
                    required = new[] { "text" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var text = args?["text"].GetString() ?? "";
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class Base64DecodeTool : ITool
{
    public string Name => "base64_decode";
    public string Description => "将Base64格式解码为文本";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        base64 = new { type = "string", description = "要解码的Base64字符串" }
                    },
                    required = new[] { "base64" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var base64 = args?["base64"].GetString() ?? "";
            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class GenerateUuidTool : ITool
{
    public string Name => "generate_uuid";
    public string Description => "生成UUID/GUID";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        count = new { type = "integer", description = "生成数量，默认1" },
                        format = new { type = "string", @enum = new[] { "default", "nodash", "uppercase" }, description = "输出格式" }
                    }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var count = args?.TryGetValue("count", out var c) == true ? c.GetInt32() : 1;
            var format = args?.TryGetValue("format", out var f) == true ? f.GetString() : "default";

            count = Math.Min(Math.Max(count, 1), 100);

            var uuids = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var uuid = Guid.NewGuid().ToString();
                uuid = format switch
                {
                    "nodash" => uuid.Replace("-", ""),
                    "uppercase" => uuid.ToUpper(),
                    _ => uuid
                };
                uuids.Add(uuid);
            }

            return string.Join("\n", uuids);
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class ConvertTimestampTool : ITool
{
    public string Name => "convert_timestamp";
    public string Description => "转换Unix时间戳与日期时间";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        value = new { type = "string", description = "时间戳或日期字符串" },
                        direction = new { type = "string", @enum = new[] { "to_date", "to_timestamp" }, description = "转换方向" }
                    },
                    required = new[] { "value", "direction" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var value = args?["value"].GetString() ?? "";
            var direction = args?["direction"].GetString() ?? "to_date";

            if (direction == "to_date")
            {
                if (long.TryParse(value, out var timestamp))
                {
                    var dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else
            {
                if (DateTime.TryParse(value, out var dt))
                {
                    return new DateTimeOffset(dt).ToUnixTimeSeconds().ToString();
                }
            }

            return "无法解析输入值";
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class CalculateExpressionTool : ITool
{
    public string Name => "calculate_expression";
    public string Description => "计算数学表达式";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        expression = new { type = "string", description = "数学表达式" }
                    },
                    required = new[] { "expression" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var expression = args?["expression"].GetString() ?? "";

            var result = new System.Data.DataTable().Compute(expression, null);
            return result?.ToString() ?? "无法计算";
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class SearchWebTool : ITool
{
    public string Name => "search_web";
    public string Description => "搜索网络获取信息（模拟）";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "搜索查询" }
                    },
                    required = new[] { "query" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        await Task.CompletedTask;
        var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
        var query = args?["query"].GetString() ?? "";
        return $"搜索 '{query}' 的结果: 此功能需要配置搜索引擎API";
    }
}

public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "读取文件内容";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "文件路径" }
                    },
                    required = new[] { "path" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var path = args?["path"].GetString() ?? "";

            if (!System.IO.File.Exists(path))
            {
                return $"错误: 文件不存在 '{path}'";
            }

            return await System.IO.File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public string Description => "写入内容到文件";

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "文件路径" },
                        content = new { type = "string", description = "文件内容" }
                    },
                    required = new[] { "path", "content" }
                }
            }
        };
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments);
            var path = args?["path"].GetString() ?? "";
            var content = args?["content"].GetString() ?? "";

            await System.IO.File.WriteAllTextAsync(path, content);
            return $"成功写入文件: {path}";
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }
}

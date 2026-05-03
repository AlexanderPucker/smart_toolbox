using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public enum SupportedLanguage
{
    Python,
    JavaScript,
    TypeScript,
    Java,
    CSharp,
    Cpp,
    Go,
    Rust,
    Ruby,
    PHP
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public TimeSpan Duration { get; set; }
    public long MemoryUsedBytes { get; set; }
    public bool TimedOut { get; set; }
}

public class CodeSandboxOptions
{
    public int TimeoutMs { get; set; } = 10000;
    public long MemoryLimitMb { get; set; } = 256;
    public bool CaptureOutput { get; set; } = true;
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public string? WorkingDirectory { get; set; }
    public List<string> Arguments { get; set; } = new();
}

public sealed class CodeSandboxService
{
    private static readonly Lazy<CodeSandboxService> _instance = new(() => new CodeSandboxService());
    public static CodeSandboxService Instance => _instance.Value;

    private readonly string _sandboxPath;
    private readonly Dictionary<SupportedLanguage, LanguageConfig> _languageConfigs;

    public event Action<SupportedLanguage, ExecutionResult>? OnCodeExecuted;

    private CodeSandboxService()
    {
        _sandboxPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartToolbox",
            "sandbox");

        Directory.CreateDirectory(_sandboxPath);

        _languageConfigs = InitializeLanguageConfigs();
    }

    private Dictionary<SupportedLanguage, LanguageConfig> InitializeLanguageConfigs()
    {
        return new Dictionary<SupportedLanguage, LanguageConfig>
        {
            [SupportedLanguage.Python] = new LanguageConfig
            {
                FileExtension = ".py",
                Command = "python3",
                Arguments = new List<string> { "{file}" },
                CheckCommand = "python3 --version"
            },
            [SupportedLanguage.JavaScript] = new LanguageConfig
            {
                FileExtension = ".js",
                Command = "node",
                Arguments = new List<string> { "{file}" },
                CheckCommand = "node --version"
            },
            [SupportedLanguage.TypeScript] = new LanguageConfig
            {
                FileExtension = ".ts",
                Command = "npx",
                Arguments = new List<string> { "ts-node", "{file}" },
                CheckCommand = "npx ts-node --version"
            },
            [SupportedLanguage.Java] = new LanguageConfig
            {
                FileExtension = ".java",
                Command = "java",
                Arguments = new List<string> { "{file}" },
                CompileCommand = "javac {file}",
                CheckCommand = "java -version"
            },
            [SupportedLanguage.CSharp] = new LanguageConfig
            {
                FileExtension = ".cs",
                Command = "dotnet",
                Arguments = new List<string> { "script", "run", "{file}" },
                CheckCommand = "dotnet --version"
            },
            [SupportedLanguage.Cpp] = new LanguageConfig
            {
                FileExtension = ".cpp",
                Command = "",
                Arguments = new List<string>(),
                CompileCommand = "g++ -o {output} {file}",
                RunCompiled = true,
                CheckCommand = "g++ --version"
            },
            [SupportedLanguage.Go] = new LanguageConfig
            {
                FileExtension = ".go",
                Command = "go",
                Arguments = new List<string> { "run", "{file}" },
                CheckCommand = "go version"
            },
            [SupportedLanguage.Rust] = new LanguageConfig
            {
                FileExtension = ".rs",
                Command = "rustc",
                Arguments = new List<string>(),
                CompileCommand = "rustc {file} -o {output}",
                RunCompiled = true,
                CheckCommand = "rustc --version"
            },
            [SupportedLanguage.Ruby] = new LanguageConfig
            {
                FileExtension = ".rb",
                Command = "ruby",
                Arguments = new List<string> { "{file}" },
                CheckCommand = "ruby --version"
            },
            [SupportedLanguage.PHP] = new LanguageConfig
            {
                FileExtension = ".php",
                Command = "php",
                Arguments = new List<string> { "{file}" },
                CheckCommand = "php --version"
            }
        };
    }

    public async Task<ExecutionResult> ExecuteAsync(
        SupportedLanguage language,
        string code,
        CodeSandboxOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new CodeSandboxOptions();

        var result = new ExecutionResult();
        var tempFile = string.Empty;
        var outputFile = string.Empty;

        try
        {
            if (!_languageConfigs.TryGetValue(language, out var config))
            {
                return new ExecutionResult
                {
                    Success = false,
                    Error = $"不支持的语言: {language}"
                };
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var sessionDir = Path.Combine(_sandboxPath, sessionId);
            Directory.CreateDirectory(sessionDir);

            tempFile = Path.Combine(sessionDir, $"code{config.FileExtension}");
            await File.WriteAllTextAsync(tempFile, code, cancellationToken);

            var workingDir = options.WorkingDirectory ?? sessionDir;

            if (!string.IsNullOrEmpty(config.CompileCommand))
            {
                outputFile = Path.Combine(sessionDir, $"output{GetExecutableExtension(language)}");
                var compileCmd = config.CompileCommand
                    .Replace("{file}", tempFile)
                    .Replace("{output}", outputFile);

                var compileResult = await RunProcessAsync(
                    compileCmd.Split(' ')[0],
                    string.Join(" ", compileCmd.Split(' ').Skip(1)),
                    workingDir,
                    options.TimeoutMs / 2,
                    cancellationToken);

                if (!compileResult.Success)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        Error = $"编译失败:\n{compileResult.Error}",
                        ExitCode = compileResult.ExitCode
                    };
                }
            }

            string command, args;

            if (config.RunCompiled && !string.IsNullOrEmpty(outputFile))
            {
                command = outputFile;
                args = string.Join(" ", options.Arguments);
            }
            else
            {
                command = config.Command;
                args = string.Join(" ", config.Arguments.Select(a => a.Replace("{file}", tempFile)));
                if (options.Arguments.Count > 0)
                {
                    args += " " + string.Join(" ", options.Arguments);
                }
            }

            result = await RunProcessAsync(command, args, workingDir, options.TimeoutMs, cancellationToken);

            CleanupSession(sessionDir);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"执行异常: {ex.Message}";
        }

        OnCodeExecuted?.Invoke(language, result);
        return result;
    }

    private async Task<ExecutionResult> RunProcessAsync(
        string command,
        string arguments,
        string workingDirectory,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        var result = new ExecutionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch { }

                result.TimedOut = true;
                result.Error = $"执行超时 (>{timeoutMs}ms)";
                result.ExitCode = -1;
                return result;
            }

            stopwatch.Stop();

            result.Output = outputBuilder.ToString();
            result.Error = errorBuilder.ToString();
            result.ExitCode = process.ExitCode;
            result.Duration = stopwatch.Elapsed;
            result.Success = process.ExitCode == 0 && string.IsNullOrEmpty(result.Error);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.Error = $"执行失败: {ex.Message}";
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }

    public async Task<bool> IsLanguageAvailableAsync(SupportedLanguage language)
    {
        if (!_languageConfigs.TryGetValue(language, out var config))
            return false;

        try
        {
            var parts = config.CheckCommand.Split(' ');
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = parts[0],
                    Arguments = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Dictionary<SupportedLanguage, bool>> CheckAllLanguagesAsync()
    {
        var results = new Dictionary<SupportedLanguage, bool>();

        foreach (var language in Enum.GetValues<SupportedLanguage>())
        {
            results[language] = await IsLanguageAvailableAsync(language);
        }

        return results;
    }

    private string GetExecutableExtension(SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Java => ".class",
            SupportedLanguage.Cpp or SupportedLanguage.Rust => "",
            _ => ".exe"
        };
    }

    private void CleanupSession(string sessionDir)
    {
        try
        {
            if (Directory.Exists(sessionDir))
            {
                Directory.Delete(sessionDir, recursive: true);
            }
        }
        catch { }
    }

    public string GetLanguageTemplate(SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.Python => @"# Python 代码示例
def main():
    print(""Hello, World!"")

if __name__ == ""__main__"":
    main()
",
            SupportedLanguage.JavaScript => @"// JavaScript 代码示例
function main() {
    console.log(""Hello, World!"");
}

main();
",
            SupportedLanguage.TypeScript => @"// TypeScript 代码示例
function main(): void {
    console.log(""Hello, World!"");
}

main();
",
            SupportedLanguage.Java => @"// Java 代码示例
public class Main {
    public static void main(String[] args) {
        System.out.println(""Hello, World!"");
    }
}
",
            SupportedLanguage.CSharp => @"// C# 代码示例
using System;

class Program {
    static void Main() {
        Console.WriteLine(""Hello, World!"");
    }
}
",
            SupportedLanguage.Go => @"// Go 代码示例
package main

import ""fmt""

func main() {
    fmt.Println(""Hello, World!"")
}
",
            SupportedLanguage.Ruby => @"# Ruby 代码示例
def main
  puts ""Hello, World!""
end

main
",
            SupportedLanguage.PHP => @"<?php
// PHP 代码示例
function main() {
    echo ""Hello, World!\n"";
}

main();
?>",
            _ => "// 请输入代码"
        };
    }
}

internal class LanguageConfig
{
    public string FileExtension { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public string? CompileCommand { get; set; }
    public string CheckCommand { get; set; } = string.Empty;
    public bool RunCompiled { get; set; }
}

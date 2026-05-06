using System.Text.Json;
using SmartToolbox.Services;
using Xunit;

namespace SmartToolbox.Tests;

public class ToolRegistryTests
{
    [Fact]
    public async Task ExecuteToolAsync_ShouldRunBuiltInTool()
    {
        var arguments = JsonSerializer.Serialize(new
        {
            text = "hello",
            algorithm = "SHA256"
        });

        var result = await ToolRegistry.Instance.ExecuteToolAsync("calculate_hash", arguments);

        Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_ShouldReturnReadableError_ForUnknownTool()
    {
        var result = await ToolRegistry.Instance.ExecuteToolAsync("missing_tool", "{}");

        Assert.Contains("未找到工具", result);
    }
}

using System.Linq;
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

    [Fact]
    public void GetToolRiskLevel_ShouldReturnRiskLevel_ForBuiltInTools()
    {
        Assert.Equal(ToolRiskLevel.Safe, ToolRegistry.Instance.GetToolRiskLevel("calculate_hash"));
        Assert.Equal(ToolRiskLevel.ReadOnly, ToolRegistry.Instance.GetToolRiskLevel("read_file"));
        Assert.Equal(ToolRiskLevel.Write, ToolRegistry.Instance.GetToolRiskLevel("write_file"));
        Assert.Equal(ToolRiskLevel.Network, ToolRegistry.Instance.GetToolRiskLevel("search_web"));
    }

    [Fact]
    public void RequiresConfirmation_ShouldOnlyRequireConfirmation_ForRiskyTools()
    {
        Assert.False(ToolRegistry.Instance.RequiresConfirmation("calculate_hash"));
        Assert.False(ToolRegistry.Instance.RequiresConfirmation("read_file"));
        Assert.True(ToolRegistry.Instance.RequiresConfirmation("write_file"));
        Assert.True(ToolRegistry.Instance.RequiresConfirmation("search_web"));
    }

    [Fact]
    public void GetToolsByRisk_ShouldReturnMatchingTools()
    {
        var safeTools = ToolRegistry.Instance.GetToolsByRisk(ToolRiskLevel.Safe);
        var writeTools = ToolRegistry.Instance.GetToolsByRisk(ToolRiskLevel.Write);

        Assert.Contains(safeTools, tool => tool.Name == "calculate_hash");
        Assert.Contains(writeTools, tool => tool.Name == "write_file");
        Assert.All(writeTools, tool => Assert.Equal(ToolRiskLevel.Write, tool.RiskLevel));
    }

    [Fact]
    public async Task ExecuteToolAsync_WithDefaultPolicy_ShouldBlockRiskyTool()
    {
        var arguments = JsonSerializer.Serialize(new
        {
            query = "smart toolbox"
        });

        var result = await ToolRegistry.Instance.ExecuteToolAsync(
            "search_web",
            arguments,
            ToolExecutionOptions.Default);

        Assert.Contains("工具执行被阻止", result);
        Assert.Contains("search_web", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_WithDefaultPolicy_ShouldAllowReadOnlyTool()
    {
        var arguments = JsonSerializer.Serialize(new
        {
            path = "missing-file.txt"
        });

        var result = await ToolRegistry.Instance.ExecuteToolAsync(
            "read_file",
            arguments,
            ToolExecutionOptions.Default);

        Assert.Contains("文件不存在", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_WithAllowAllPolicy_ShouldAllowRiskyTool()
    {
        var arguments = JsonSerializer.Serialize(new
        {
            query = "smart toolbox"
        });

        var result = await ToolRegistry.Instance.ExecuteToolAsync(
            "search_web",
            arguments,
            ToolExecutionOptions.AllowAll);

        Assert.Contains("需要配置搜索引擎API", result);
    }
}

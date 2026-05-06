using SmartToolbox.Services;
using Xunit;

namespace SmartToolbox.Tests;

public class ModelRouterTests
{
    [Fact]
    public void GetModelInfo_ShouldExposeFunctionCallingCapability()
    {
        var model = ModelRouter.Instance.GetModelInfo("gpt-4o-mini");

        Assert.NotNull(model);
        Assert.True(model!.SupportsFunctionCalling);
    }
}

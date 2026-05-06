using System.Globalization;
using SmartToolbox.ViewModels;
using Xunit;

namespace SmartToolbox.Tests;

public class RoleToNameConverterTests
{
    [Theory]
    [InlineData("user", "你")]
    [InlineData("assistant", "AI")]
    [InlineData("tool", "工具")]
    public void Convert_ShouldMapKnownRoles(string role, string expected)
    {
        var converter = new RoleToNameConverter();

        var result = converter.Convert(role, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }
}

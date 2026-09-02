using ParagLog.Core.Common;

namespace ParagLog.UnitTests.Common;

public class PathSafetyTests
{
    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("Con")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("com9")]
    [InlineData("LPT1")]
    [InlineData("lpt9")]
    [InlineData("con.txt")]
    public void IsReservedName_detects_windows_device_names(string name)
    {
        Assert.True(PathSafety.IsReservedName(name));
    }

    [Theory]
    [InlineData("constable")]
    [InlineData("bob")]
    [InlineData("comedy")]
    [InlineData("lpt10")]
    public void IsReservedName_allows_ordinary_names(string name)
    {
        Assert.False(PathSafety.IsReservedName(name));
    }

    [Theory]
    [InlineData("bob.")]
    [InlineData("bob ")]
    public void HasUnsafeTrailingCharacters_detects_trailing_dot_or_space(string name)
    {
        Assert.True(PathSafety.HasUnsafeTrailingCharacters(name));
    }

    [Fact]
    public void HasUnsafeTrailingCharacters_allows_ordinary_names()
    {
        Assert.False(PathSafety.HasUnsafeTrailingCharacters("bob-ab12x"));
    }
}

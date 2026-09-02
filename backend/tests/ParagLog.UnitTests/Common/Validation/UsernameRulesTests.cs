using ParagLog.Core.Common.Validation;

namespace ParagLog.UnitTests.Common.Validation;

public class UsernameRulesTests
{
    [Theory]
    [InlineData("bob")]
    [InlineData("Bob_123")]
    [InlineData("a-b-c-d-e-f-g-h-i-j")] // 20 chars
    [InlineData("abc")] // 3 chars, minimum
    public void IsValid_accepts_conforming_usernames(string username)
    {
        Assert.True(UsernameRules.IsValid(username));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")] // too short
    [InlineData("a-b-c-d-e-f-g-h-i-j-k")] // 21 chars, too long
    [InlineData("bob smith")] // space
    [InlineData("bob@smith")] // invalid character
    [InlineData("bob.smith")] // dot
    public void IsValid_rejects_nonconforming_usernames(string username)
    {
        Assert.False(UsernameRules.IsValid(username));
    }
}

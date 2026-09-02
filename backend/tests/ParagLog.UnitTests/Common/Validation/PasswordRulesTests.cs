using ParagLog.Core.Common.Validation;

namespace ParagLog.UnitTests.Common.Validation;

public class PasswordRulesTests
{
    [Fact]
    public void HasMinLength_rejects_short_passwords()
    {
        Assert.False(PasswordRules.HasMinLength(new string('a', PasswordRules.MinLength - 1)));
    }

    [Fact]
    public void HasMinLength_accepts_passwords_at_minimum_length()
    {
        Assert.True(PasswordRules.HasMinLength(new string('a', PasswordRules.MinLength)));
    }

    [Theory]
    [InlineData("password1234")]
    [InlineData("qwertyuiop123")]
    [InlineData("123456789012")]
    public void IsCommonPassword_detects_listed_passwords(string password)
    {
        Assert.True(PasswordRules.IsCommonPassword(password));
    }

    [Fact]
    public void IsCommonPassword_allows_unlisted_passwords()
    {
        Assert.False(PasswordRules.IsCommonPassword("xk7#mQ2!vLpz9Fjw"));
    }
}

using ParagLog.Core.Common.Validation;

namespace ParagLog.UnitTests.Common.Validation;

public class EmailRulesTests
{
    [Theory]
    [InlineData("bob@example.com")]
    [InlineData("bob.smith+tag@example.co.uk")]
    public void IsValid_accepts_conforming_emails(string email)
    {
        Assert.True(EmailRules.IsValid(email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("bob@")]
    [InlineData("@example.com")]
    [InlineData("bob smith@example.com")]
    public void IsValid_rejects_nonconforming_emails(string email)
    {
        Assert.False(EmailRules.IsValid(email));
    }
}

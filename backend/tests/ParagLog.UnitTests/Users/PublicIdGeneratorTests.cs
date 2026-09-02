using ParagLog.Core.Common;
using ParagLog.Core.Users;

namespace ParagLog.UnitTests.Users;

public class PublicIdGeneratorTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private const string ServerSalt = "test-salt";

    [Fact]
    public void Generate_is_deterministic_for_the_same_inputs()
    {
        var first = PublicIdGenerator.Generate("Bob", CreatedAt, ServerSalt);
        var second = PublicIdGenerator.Generate("Bob", CreatedAt, ServerSalt);

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("Bob")]
    [InlineData("BOB")]
    public void Generate_is_lowercase(string username)
    {
        var publicId = PublicIdGenerator.Generate(username, CreatedAt, ServerSalt);

        Assert.Equal(publicId.ToLowerInvariant(), publicId);
    }

    [Fact]
    public void Generate_differs_when_the_creation_time_differs()
    {
        var first = PublicIdGenerator.Generate("bob", CreatedAt, ServerSalt);
        var second = PublicIdGenerator.Generate("bob", CreatedAt.AddSeconds(1), ServerSalt);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Generate_differs_when_the_server_salt_differs()
    {
        var first = PublicIdGenerator.Generate("bob", CreatedAt, "salt-a");
        var second = PublicIdGenerator.Generate("bob", CreatedAt, "salt-b");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Generate_starts_with_the_username_slug()
    {
        var publicId = PublicIdGenerator.Generate("Bob-Smith_99", CreatedAt, ServerSalt);

        Assert.StartsWith("bob-smith-99-", publicId);
    }

    [Theory]
    [InlineData("con")]
    [InlineData("CON")]
    [InlineData("prn")]
    [InlineData("com1")]
    [InlineData("lpt9")]
    public void Generate_never_produces_a_reserved_device_name(string username)
    {
        var publicId = PublicIdGenerator.Generate(username, CreatedAt, ServerSalt);

        Assert.False(PathSafety.IsReservedName(publicId));
    }

    [Fact]
    public void Generate_never_has_unsafe_trailing_characters()
    {
        var publicId = PublicIdGenerator.Generate("bob", CreatedAt, ServerSalt);

        Assert.False(PathSafety.HasUnsafeTrailingCharacters(publicId));
    }
}

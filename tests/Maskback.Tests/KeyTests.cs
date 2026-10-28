using Maskback.Detect;

namespace Maskback.Tests;

public sealed class KeyTests
{
    [Theory]
    [InlineData("sk-abcdefghijklmnopqrst", true)]
    [InlineData("ghp_abcdefghijklmnopqrstuvwxyz0123456789", true)]
    [InlineData("AKIAABCDEFGHIJKLMNOP", true)]
    [InlineData("sk-abcdefghijklmnopqrs", false)]
    [InlineData("sk-abcdefghijklmnopqrst!", false)]
    [InlineData("ghp_abcdefghijklmnopqrstuvwxyz012345678", false)]
    [InlineData("ghp_abcdefghijklmnopqrstuvwxyz01234567890", false)]
    [InlineData("AKIAabcdefghijklmnop", false)]
    [InlineData("SK-abcdefghijklmnopqrst", false)]
    [InlineData("xsk-abcdefghijklmnopqrst", false)]
    [InlineData("sk-abcdefghijklmnopqrstuvwxyz_0123456789-", true)]
    [InlineData("ghp_abcdefghijklmnopqrstuvwxyz012345678_", false)]
    [InlineData("AKIAABCDEFGHIJKLMNO", false)]
    [InlineData("AKIAABCDEFGHIJKLMNOPQ", false)]
    [InlineData("sk-abcdefghijklmnopqrsé", false)]
    public void MatchesOnlySupportedKeyFormats(string word, bool expected)
    {
        Assert.Equal(expected, Key.Matches(word));
    }
}

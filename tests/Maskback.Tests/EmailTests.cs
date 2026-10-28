using Maskback.Detect;

namespace Maskback.Tests;

public sealed class EmailTests
{
    [Theory]
    [InlineData("a@example.com", true)]
    [InlineData("bob.smith@mail.example.org", true)]
    [InlineData("x+tag@sub.domain.io", true)]
    [InlineData("a_b%c@x-y.co", true)]
    [InlineData("A@B.CO", true)]
    [InlineData("1@2.ab", true)]
    [InlineData("a@b", false)]
    [InlineData("@x.com", false)]
    [InlineData("a@-b.com", false)]
    [InlineData("a@b-.com", false)]
    [InlineData("a..b@x.com", false)]
    [InlineData(".a@x.com", false)]
    [InlineData("a.@x.com", false)]
    [InlineData("a@x..com", false)]
    [InlineData("a@@x.com", false)]
    [InlineData("a@x.c", false)]
    [InlineData("a@x.c1", false)]
    [InlineData("café@x.com", false)]
    [InlineData("a@x.com.", false)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@x.com", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@x.com", false)]
    [InlineData("a@bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.co", true)]
    [InlineData("a@bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.co", false)]
    [InlineData("", false)]
    public void MatchesOnlyValidEmailAddresses(string word, bool expected)
    {
        Assert.Equal(expected, Email.Matches(word));
    }
}

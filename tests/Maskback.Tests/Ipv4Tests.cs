using Maskback.Detect;

namespace Maskback.Tests;

public sealed class Ipv4Tests
{
    [Theory]
    [InlineData("0.0.0.0", true)]
    [InlineData("192.168.1.10", true)]
    [InlineData("255.255.255.255", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("1.2.3.4", true)]
    [InlineData("256.1.1.1", false)]
    [InlineData("1.2.3", false)]
    [InlineData("1.2.3.4.5", false)]
    [InlineData("01.2.3.4", false)]
    [InlineData("1..2.3", false)]
    [InlineData("1.2.3.", false)]
    [InlineData("a.b.c.d", false)]
    [InlineData("1.2.3.1000", false)]
    [InlineData("١.2.3.4", false)]
    [InlineData("", false)]
    [InlineData("-1.2.3.4", false)]
    public void MatchesOnlyDottedQuads(string word, bool expected)
    {
        Assert.Equal(expected, Ipv4.Matches(word));
    }
}

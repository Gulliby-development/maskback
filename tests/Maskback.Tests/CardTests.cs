using Maskback.Detect;

namespace Maskback.Tests;

public sealed class CardTests
{
    [Theory]
    [InlineData("4111111111111111", true)]
    [InlineData("4111-1111-1111-1111", true)]
    [InlineData("378282246310005", true)]
    [InlineData("5555555555554444", true)]
    [InlineData("4222222222222", true)]
    [InlineData("4111-11111111-1111", true)]
    [InlineData("4111111111111112", false)]
    [InlineData("411111111111", false)]
    [InlineData("41111111111111111111", false)]
    [InlineData("-4111111111111111", false)]
    [InlineData("4111111111111111-", false)]
    [InlineData("4111--1111-1111-1111", false)]
    [InlineData("4111 1111 1111 1111", false)]
    [InlineData("", false)]
    [InlineData("0000000000000000000", true)]
    [InlineData("0000000000000", true)]
    [InlineData("４111111111111111", false)]
    [InlineData("411111111111111a", false)]
    [InlineData("000000000000", false)]
    public void MatchesOnlyLuhnValidCardNumbers(string word, bool expected)
    {
        Assert.Equal(expected, Card.Matches(word));
    }
}

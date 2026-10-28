using Maskback.Detect;

namespace Maskback.Tests;

public sealed class IbanTests
{
    [Theory]
    [InlineData("GB82WEST12345698765432", true)]
    [InlineData("DE89370400440532013000", true)]
    [InlineData("FR1420041010050500013M02606", true)]
    [InlineData("NL91ABNA0417164300", true)]
    [InlineData("NO9386011117947", true)]
    [InlineData("GB82WEST12345698765431", false)]
    [InlineData("gb82west12345698765432", false)]
    [InlineData("GB8AWEST12345698765432", false)]
    [InlineData("1B82WEST12345698765432", false)]
    [InlineData("GB82-WEST12345698765432", false)]
    [InlineData("XX00AAAA00000000", false)]
    [InlineData("GB16AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", true)]
    [InlineData("NO938601111794", false)]
    [InlineData("GB16AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", false)]
    public void MatchesOnlyValidIbans(string word, bool expected)
    {
        Assert.Equal(expected, Iban.Matches(word));
    }
}

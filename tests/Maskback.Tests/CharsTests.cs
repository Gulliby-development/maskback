using Maskback.Detect;

namespace Maskback.Tests;

public sealed class CharsTests
{
    [Theory]
    [InlineData('0', true, false, false, true)]
    [InlineData('9', true, false, false, true)]
    [InlineData('A', false, true, true, true)]
    [InlineData('Z', false, true, true, true)]
    [InlineData('a', false, false, true, true)]
    [InlineData('z', false, false, true, true)]
    [InlineData('/', false, false, false, false)]
    [InlineData(':', false, false, false, false)]
    [InlineData('@', false, false, false, false)]
    [InlineData('[', false, false, false, false)]
    [InlineData('\u00E9', false, false, false, false)]
    [InlineData('\u0661', false, false, false, false)]
    public void ClassifiesOnlyAsciiCharacters(char character, bool isDigit, bool isUpper, bool isLetter, bool isAlnum)
    {
        Assert.Equal(isDigit, Chars.IsDigit(character));
        Assert.Equal(isUpper, Chars.IsUpper(character));
        Assert.Equal(isLetter, Chars.IsLetter(character));
        Assert.Equal(isAlnum, Chars.IsAlnum(character));
    }
}

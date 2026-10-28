using Maskback.Model;

namespace Maskback.Tests;

public sealed class WordsTests
{
    [Theory]
    [InlineData("  x  ", 2, "x")]
    [InlineData("(a@b.co).", 1, "a@b.co")]
    [InlineData("[<{(a)}>]", 4, "a")]
    [InlineData("a.b.", 0, "a.b")]
    [InlineData("a\u00A0b", 0, "a\u00A0b")]
    public void SplitsAndTrimsOneWord(string text, int start, string word)
    {
        Assert.Equal([(start, word)], Words.Split(text));
    }

    [Theory]
    [InlineData("a bc", 0, "a", 2, "bc")]
    [InlineData("\"q\" 'r'", 1, "q", 5, "r")]
    [InlineData("((a))\n(b)", 2, "a", 7, "b")]
    [InlineData("😀 x", 0, "😀", 3, "x")]
    public void SplitsTwoWordsWithUtf16Offsets(string text, int firstStart, string firstWord, int secondStart, string secondWord)
    {
        Assert.Equal([(firstStart, firstWord), (secondStart, secondWord)], Words.Split(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("...")]
    public void OmitsEmptyWords(string text)
    {
        Assert.Empty(Words.Split(text));
    }

    [Fact]
    public void SplitsTabAndLineSeparators()
    {
        Assert.Equal([(0, "a"), (2, "b"), (5, "c")], Words.Split("a\tb\r\nc"));
    }
}

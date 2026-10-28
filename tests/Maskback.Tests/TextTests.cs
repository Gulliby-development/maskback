using Maskback.Base;

namespace Maskback.Tests;

public sealed class TextTests
{
    [Theory]
    [InlineData(new byte[] { }, "", false)]
    [InlineData(new byte[] { 0x61 }, "a", false)]
    [InlineData(new byte[] { 0xC3, 0xA9 }, "é", false)]
    [InlineData(new byte[] { 0xF0, 0x9F, 0x98, 0x80 }, "😀", false)]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x61 }, "a", false)]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0xEF, 0xBB, 0xBF }, "\uFEFF", false)]
    [InlineData(new byte[] { 0xFF }, "in.txt: input is not valid UTF-8", true)]
    [InlineData(new byte[] { 0xC3 }, "in.txt: input is not valid UTF-8", true)]
    [InlineData(new byte[] { 0xC0, 0xAF }, "in.txt: input is not valid UTF-8", true)]
    [InlineData(new byte[] { 0xED, 0xA0, 0x80 }, "in.txt: input is not valid UTF-8", true)]
    public void DecodesStrictUtf8(byte[] bytes, string expected, bool isInvalid)
    {
        if (isInvalid)
        {
            var exception = Assert.Throws<FailureException>(() => Text.Decode(bytes, "in.txt"));
            Assert.Equal(2, exception.Code);
            Assert.Equal(expected, exception.Message);
            return;
        }

        Assert.Equal(expected, Text.Decode(bytes, "in.txt"));
    }

    [Theory]
    [InlineData("", new string[] { })]
    [InlineData("a", new string[] { "a" })]
    [InlineData("a\n", new string[] { "a" })]
    [InlineData("\n", new string[] { "" })]
    [InlineData("a\n\nb", new string[] { "a", "", "b" })]
    [InlineData("a\r\nb\r", new string[] { "a", "b\r" })]
    [InlineData("a\r\r\n", new string[] { "a\r" })]
    public void SplitsLinesWithSpecifiedBoundaries(string text, string[] expected)
    {
        Assert.Equal(expected, Text.SplitLines(text));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("a\tb", "a\tb")]
    [InlineData("\\", "\\\\")]
    [InlineData("\n\r", "\\n\\r")]
    [InlineData("\\n\n", "\\\\n\\n")]
    [InlineData("abc", "abc")]
    public void EscapesOnlyOriginalSpecialCharacters(string text, string expected)
    {
        Assert.Equal(expected, Text.Escape(text));
    }
}

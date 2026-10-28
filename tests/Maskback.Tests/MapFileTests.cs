using Maskback.Base;
using Maskback.Core;

namespace Maskback.Tests;

public sealed class MapFileTests
{
    [Theory]
    [InlineData(false, false, "", "", "", "", "maskback-map 1\n")]
    [InlineData(true, false, "{{EMAIL_1}}", "a@b.co", "", "", "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n")]
    [InlineData(true, true, "{{KEY_2}}", "second", "{{EMAIL_1}}", "first", "maskback-map 1\n{{KEY_2}}\tsecond\n{{EMAIL_1}}\tfirst\n")]
    public void WritesHeaderAndRecordsInEnumerationOrder(
        bool hasFirst,
        bool hasSecond,
        string placeholder1,
        string original1,
        string placeholder2,
        string original2,
        string expected)
    {
        List<(string Placeholder, string Original)> table = [];
        if (hasFirst)
        {
            table.Add((placeholder1, original1));
            if (hasSecond)
            {
                table.Add((placeholder2, original2));
            }
        }

        Assert.Equal(expected, MapFile.Write(table));
        if (!hasFirst)
        {
            Assert.Equal("maskback-map 1\n{{EMAIL_28}}\t28\n", MapFile.Write([("{{EMAIL_28}}", "28")]));
        }
    }

    [Theory]
    [InlineData("maskback-map 1", 0, null, null, null, null)]
    [InlineData("maskback-map 1\n", 0, null, null, null, null)]
    [InlineData("maskback-map 1\r\n{{EMAIL_1}}\ta@b.co\r\n", 1, "{{EMAIL_1}}", "a@b.co", null, null)]
    [InlineData("maskback-map 1\n{{EMAIL_1}}\t a@b.co \n{{KEY_1}}\tx", 2, "{{EMAIL_1}}", " a@b.co ", "{{KEY_1}}", "x")]
    public void ReadsValidMapLinesWithoutTrimming(
        string text,
        int expectedCount,
        string? expectedPlaceholder1,
        string? expectedOriginal1,
        string? expectedPlaceholder2,
        string? expectedOriginal2)
    {
        var actual = MapFile.Read(text, "m.txt");

        Assert.Equal(expectedCount, actual.Count);
        if (expectedPlaceholder1 is not null)
        {
            Assert.Equal(expectedOriginal1, actual[expectedPlaceholder1]);
        }

        if (expectedPlaceholder2 is not null)
        {
            Assert.Equal(expectedOriginal2, actual[expectedPlaceholder2]);
        }
    }

    [Theory]
    [InlineData("", "m.txt: not a maskback map")]
    [InlineData("maskback-map 2\n", "m.txt: not a maskback map")]
    [InlineData("\uFEFFmaskback-map 1\n", "m.txt: not a maskback map")]
    [InlineData("maskback-map 1\n{{EMAIL_1}}\n", "m.txt:2: bad map line")]
    [InlineData("maskback-map 1\n{{EMAIL_1}}\tx\ty\n", "m.txt:2: bad map line")]
    [InlineData("maskback-map 1\n{{EMAIL_1}}\t\n", "m.txt:2: bad map line")]
    [InlineData("maskback-map 1\n{{email_1}}\tx\n", "m.txt:2: bad map line")]
    [InlineData("maskback-map 1\n{{EMAIL_1}}\tx\n\n", "m.txt:3: bad map line")]
    [InlineData("maskback-map 1\r\n{{EMAIL_1}}\tx\r\n{{EMAIL_1}}\ty\r\n", "m.txt:3: duplicate placeholder {{EMAIL_1}}")]
    public void RejectsInvalidMapsWithExactDiagnostics(string text, string expectedMessage)
    {
        var exception = Assert.Throws<FailureException>(() => MapFile.Read(text, "m.txt"));

        Assert.Equal(2, exception.Code);
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("{{EMAIL_1}}", true, new string[] { "{{EMAIL_1}}" })]
    [InlineData("{{A1_999999999}}", true, new string[] { "{{A1_999999999}}" })]
    [InlineData("x{{EMAIL_1}}y", false, new string[] { "{{EMAIL_1}}" })]
    [InlineData("{{email_1}}", false, new string[] { })]
    [InlineData("{{EMAIL_01}}", false, new string[] { })]
    [InlineData("{{ EMAIL_1 }}", false, new string[] { })]
    [InlineData("{{EMAIL_1}", false, new string[] { })]
    [InlineData("{{EMAIL_1234567890}}", false, new string[] { })]
    [InlineData("{{EMAIL_0}}", false, new string[] { })]
    [InlineData("{{ÉMAIL_1}}", false, new string[] { })]
    [InlineData("{{EMAIL_\u0661}}", false, new string[] { })]
    [InlineData("{{EMAIL_1}}\n", false, new string[] { "{{EMAIL_1}}" })]
    public void MatchesOnlyExactPlaceholderSyntax(string text, bool expectedWhole, string[] expectedMatches)
    {
        Assert.Equal(expectedWhole, Placeholders.Whole().IsMatch(text));
        Assert.Equal(expectedMatches, Placeholders.Any().Matches(text).Select(match => match.Value));
    }
}

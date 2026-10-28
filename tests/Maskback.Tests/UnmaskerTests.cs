using Maskback.Core;

namespace Maskback.Tests;

public sealed class UnmaskerTests
{
    [Fact]
    public void UnmasksEmptyText()
    {
        var (Text, Unknown) = Unmasker.Unmask("", new Dictionary<string, string>());
        Assert.Equal("", Text);
        Assert.Equal([], Unknown);
    }

    [Fact]
    public void ReplacesKnownPlaceholder()
    {
        var (Text, Unknown) = Unmasker.Unmask(
            "Dear {{EMAIL_1}}.",
            new Dictionary<string, string> { ["{{EMAIL_1}}"] = "a@b.co" });
        Assert.Equal("Dear a@b.co.", Text);
        Assert.Equal([], Unknown);
    }

    [Fact]
    public void ReplacesRepeatedKnownPlaceholder()
    {
        var (Text, Unknown) = Unmasker.Unmask(
            "{{EMAIL_1}}/{{EMAIL_1}}",
            new Dictionary<string, string> { ["{{EMAIL_1}}"] = "a@b.co" });
        Assert.Equal("a@b.co/a@b.co", Text);
        Assert.Equal([], Unknown);
    }

    [Fact]
    public void ReportsDistinctUnknownsInEncounterOrder()
    {
        var (Text, Unknown) = Unmasker.Unmask("{{EMAIL_7}} {{KEY_2}} {{EMAIL_7}}", new Dictionary<string, string>());
        Assert.Equal("{{EMAIL_7}} {{KEY_2}} {{EMAIL_7}}", Text);
        Assert.Equal(["{{EMAIL_7}}", "{{KEY_2}}"], Unknown);
    }

    [Fact]
    public void RestoresKnownPlaceholdersAndReportsUnknowns()
    {
        var (Text, Unknown) = Unmasker.Unmask(
            "{{EMAIL_7}} {{EMAIL_1}} {{KEY_2}}",
            new Dictionary<string, string> { ["{{EMAIL_1}}"] = "a@b.co" });
        Assert.Equal("{{EMAIL_7}} a@b.co {{KEY_2}}", Text);
        Assert.Equal(["{{EMAIL_7}}", "{{KEY_2}}"], Unknown);
    }

    [Fact]
    public void LeavesMalformedPlaceholderLikeTextUnchanged()
    {
        var text = "{{email_1}} {{EMAIL_01}} {{ EMAIL_1 }} {{EMAIL_1} {{EMAIL_1234567890}}";
        var (Text, Unknown) = Unmasker.Unmask(text, new Dictionary<string, string>());
        Assert.Equal("{{email_1}} {{EMAIL_01}} {{ EMAIL_1 }} {{EMAIL_1} {{EMAIL_1234567890}}", Text);
        Assert.Equal([], Unknown);
    }

    [Fact]
    public void InsertsReplacementLiterallyWithoutRescanning()
    {
        var (Text, Unknown) = Unmasker.Unmask(
            "{{EMAIL_1}}",
            new Dictionary<string, string>
            {
                ["{{EMAIL_1}}"] = "{{KEY_1}} $1",
                ["{{KEY_1}}"] = "secret",
            });
        Assert.Equal("{{KEY_1}} $1", Text);
        Assert.Equal([], Unknown);
    }

    [Fact]
    public void PreservesSurroundingWhitespaceAndLineEndings()
    {
        var (Text, Unknown) = Unmasker.Unmask(
            "  x\r\n{{EMAIL_1}}\n\t",
            new Dictionary<string, string> { ["{{EMAIL_1}}"] = "a@b.co" });
        Assert.Equal("  x\r\na@b.co\n\t", Text);
        Assert.Equal([], Unknown);
    }
}

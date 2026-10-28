using Maskback.Core;
using Maskback.Model;

namespace Maskback.Tests;

public sealed class ScannerTests
{
    [Fact]
    public void ReturnsNoFindingsForEmptyText()
    {
        Assert.Empty(Scanner.Scan("", Kinds.All));
    }

    [Fact]
    public void ReturnsNoFindingsForUnmatchedText()
    {
        Assert.Empty(Scanner.Scan("hello world", Kinds.All));
    }

    [Theory]
    [InlineData("(a@b.co).", "all", 1, "email", "a@b.co")]
    [InlineData("😀 a@b.co", "all", 3, "email", "a@b.co")]
    [InlineData("a@b.co 1.2.3.4", "ipv4", 7, "ipv4", "1.2.3.4")]
    public void FindsEnabledMatchAtExactOffset(string text, string kindSelection, int expectedStart, string expectedKind, string expectedValue)
    {
        IReadOnlyCollection<string> kinds = kindSelection == "all" ? Kinds.All : [kindSelection];
        var finding = Assert.Single(Scanner.Scan(text, kinds));
        Assert.Equal(new Finding(expectedStart, expectedKind, expectedValue), finding);
    }

    [Fact]
    public void ReturnsAllDetectorMatchesInSourceOrder()
    {
        Assert.Equal(
            [
                new Finding(0, "email", "a@b.co"),
                new Finding(7, "key", "sk-abcdefghijklmnopqrst"),
                new Finding(31, "iban", "GB82WEST12345698765432"),
                new Finding(54, "card", "4111111111111111"),
                new Finding(71, "ipv4", "1.2.3.4")
            ],
            Scanner.Scan("a@b.co\nsk-abcdefghijklmnopqrst\nGB82WEST12345698765432\n4111111111111111\n1.2.3.4", ["ipv4", "card", "iban", "key", "email"]));
    }

    [Fact]
    public void DuplicateEnabledKindsDoNotDuplicateFindings()
    {
        Assert.Equal(
            [new Finding(0, "email", "a@b.co"), new Finding(7, "email", "a@b.co")],
            Scanner.Scan("a@b.co a@b.co", ["email", "email"]));
    }

    [Fact]
    public void ReturnsNoFindingsWhenNoKindsAreEnabled()
    {
        Assert.Empty(Scanner.Scan("a@b.co", []));
    }
}

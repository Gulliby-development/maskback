using System.Globalization;
using Maskback.Core;

namespace Maskback.Tests;

public sealed class MaskerTests
{
    [Fact]
    public void ReusesRepeatedPlaceholderIdentity()
    {
        var masker = new Masker();

        Assert.Equal("{{EMAIL_1}}", masker.Placeholder("email", "a@b.co"));
        Assert.Equal("{{EMAIL_1}}", masker.Placeholder("email", "a@b.co"));
        Assert.Collection(masker.Table, entry => Assert.Equal(("{{EMAIL_1}}", "a@b.co"), entry));
    }

    [Fact]
    public void KeepsPairIdentityAndIndependentKindCounters()
    {
        var masker = new Masker();

        Assert.Equal("{{EMAIL_1}}", masker.Placeholder("email", "same"));
        Assert.Equal("{{KEY_1}}", masker.Placeholder("key", "same"));
        Assert.Equal("{{EMAIL_2}}", masker.Placeholder("email", "other"));
        Assert.Collection(
            masker.Table,
            entry => Assert.Equal(("{{EMAIL_1}}", "same"), entry),
            entry => Assert.Equal(("{{KEY_1}}", "same"), entry),
            entry => Assert.Equal(("{{EMAIL_2}}", "other"), entry));
    }

    [Fact]
    public void ComparesOriginalsOrdinally()
    {
        var masker = new Masker();

        Assert.Equal("{{EMAIL_1}}", masker.Placeholder("email", "A@B.CO"));
        Assert.Equal("{{EMAIL_2}}", masker.Placeholder("email", "a@b.co"));
        Assert.Collection(
            masker.Table,
            entry => Assert.Equal(("{{EMAIL_1}}", "A@B.CO"), entry),
            entry => Assert.Equal(("{{EMAIL_2}}", "a@b.co"), entry));
    }

    [Fact]
    public void PersistsPlaceholdersAcrossDirectAndMaskCalls()
    {
        var masker = new Masker();
        var kinds = new[] { "email" };

        Assert.Equal("{{EMAIL_1}}", masker.Placeholder("email", "a@b.co"));
        Assert.Equal("{{EMAIL_1}} {{EMAIL_2}}", masker.Mask("a@b.co c@d.co", kinds));
        Assert.Equal("{{EMAIL_2}} {{EMAIL_3}} {{EMAIL_1}}", masker.Mask("c@d.co e@f.co a@b.co", kinds));
        Assert.Collection(
            masker.Table,
            entry => Assert.Equal(("{{EMAIL_1}}", "a@b.co"), entry),
            entry => Assert.Equal(("{{EMAIL_2}}", "c@d.co"), entry),
            entry => Assert.Equal(("{{EMAIL_3}}", "e@f.co"), entry));
    }

    [Theory]
    [InlineData(
        "😀 (a@b.co).\r\nsk-abcdefghijklmnopqrst\tGB82WEST12345698765432\r4111111111111111 1.2.3.4\n",
        new string[] { "email", "key", "iban", "card", "ipv4" },
        "😀 ({{EMAIL_1}}).\r\n{{KEY_1}}\t{{IBAN_1}}\r{{CARD_1}} {{IPV4_1}}\n",
        new string[]
        {
            "{{EMAIL_1}}\ta@b.co",
            "{{KEY_1}}\tsk-abcdefghijklmnopqrst",
            "{{IBAN_1}}\tGB82WEST12345698765432",
            "{{CARD_1}}\t4111111111111111",
            "{{IPV4_1}}\t1.2.3.4"
        })]
    [InlineData(
        "a@b.co 1.2.3.4",
        new string[] { "email" },
        "{{EMAIL_1}} 1.2.3.4",
        new string[] { "{{EMAIL_1}}\ta@b.co" })]
    [InlineData("", new string[] { "email", "key", "iban", "card", "ipv4" }, "", new string[] { })]
    [InlineData(
        "plain\r\n\ttext!",
        new string[] { "email", "key", "iban", "card", "ipv4" },
        "plain\r\n\ttext!",
        new string[] { })]
    public void MasksOnlyFindingsAndPreservesOtherCharacters(
        string text,
        string[] kinds,
        string expected,
        string[] expectedTable)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var masker = new Masker();

            Assert.Equal(expected, masker.Mask(text, kinds));
            Assert.Equal(expectedTable, masker.Table.Select(entry => entry.Placeholder + "\t" + entry.Original));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}

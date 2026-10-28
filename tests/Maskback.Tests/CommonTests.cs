using Maskback.Base;
using Maskback.Cmd;

namespace Maskback.Tests;

public sealed class CommonTests
{
    [Theory]
    [InlineData(null, "email,key,iban,card,ipv4")]
    [InlineData("ipv4,email", "ipv4,email")]
    [InlineData("email,email", "email,email")]
    [InlineData("card", "card")]
    public void ParseKindsUsesDefaultOrPreservesAllowedValues(string? kinds, string expected)
    {
        var parsed = new Parsed();
        if (kinds is not null)
        {
            parsed.Options.Add("--kinds", kinds);
        }

        Assert.Equal(expected, string.Join(',', Common.ParseKinds(parsed)));
    }

    [Theory]
    [InlineData("phone", "unknown kind phone")]
    [InlineData("EMAIL", "unknown kind EMAIL")]
    [InlineData("", "unknown kind ")]
    [InlineData("email,", "unknown kind ")]
    [InlineData("email, ipv4", "unknown kind  ipv4")]
    public void ParseKindsRejectsUnknownValues(string kinds, string expectedMessage)
    {
        var parsed = new Parsed();
        parsed.Options.Add("--kinds", kinds);

        var exception = Assert.Throws<FailureException>(() => Common.ParseKinds(parsed));

        Assert.Equal(2, exception.Code);
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void WarningsReturnsEmptyTextForNoUnknowns()
    {
        Assert.Equal(string.Empty, Common.Warnings([]));
    }

    [Fact]
    public void WarningsPreservesOrderAndAddsLineFeeds()
    {
        Assert.Equal(
            "maskback: unknown placeholder {{EMAIL_7}}\nmaskback: unknown placeholder {{KEY_2}}\n",
            Common.Warnings(["{{EMAIL_7}}", "{{KEY_2}}"]));
    }

    [Fact]
    public void WarningsAndNumericUnknownKindsHaveExactDiagnostics()
    {
        Assert.Equal(
            "maskback: unknown placeholder {{EMAIL_7}}\n",
            Common.Warnings(["{{EMAIL_7}}"]));

        var parsed = new Parsed();
        parsed.Options.Add("--kinds", "12");
        var twelveException = Assert.Throws<FailureException>(() => Common.ParseKinds(parsed));
        Assert.Equal(2, twelveException.Code);
        Assert.Equal("unknown kind 12", twelveException.Message);

        parsed.Options["--kinds"] = "10";
        var tenException = Assert.Throws<FailureException>(() => Common.ParseKinds(parsed));
        Assert.Equal(2, tenException.Code);
        Assert.Equal("unknown kind 10", tenException.Message);
    }
}

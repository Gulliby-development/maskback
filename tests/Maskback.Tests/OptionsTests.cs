using Maskback.Base;

namespace Maskback.Tests;

public sealed class OptionsTests
{
    private static readonly string[] FlagNames = ["--map"];
    private static readonly string[] SwitchNames = ["--strict"];

    [Theory]
    [InlineData(new[] { "a", "-", "--map", "m", "--strict" }, 2, "a", "-", 2, "--map", "m", "--strict", "")]
    [InlineData(new[] { "--map=m=n" }, 0, "", "", 1, "--map", "m=n", "", "")]
    [InlineData(new[] { "--map=" }, 0, "", "", 1, "--map", "", "", "")]
    [InlineData(new[] { "--", "--map", "-" }, 2, "--map", "-", 0, "", "", "", "")]
    public void ParsesPositionalsAndOptions(
        string[] args,
        int expectedPositionalCount,
        string expectedPositional1,
        string expectedPositional2,
        int expectedOptionCount,
        string expectedOption1Name,
        string expectedOption1Value,
        string expectedOption2Name,
        string expectedOption2Value)
    {
        var parsed = Options.Parse(args, FlagNames, SwitchNames);

        Assert.Equal(expectedPositionalCount, parsed.Positionals.Count);
        if (expectedPositionalCount > 0)
        {
            Assert.Equal(expectedPositional1, parsed.Positionals[0]);
        }

        if (expectedPositionalCount > 1)
        {
            Assert.Equal(expectedPositional2, parsed.Positionals[1]);
        }

        Assert.Equal(expectedOptionCount, parsed.Options.Count);
        if (expectedOptionCount > 0)
        {
            Assert.Equal(expectedOption1Value, parsed.Options[expectedOption1Name]);
        }

        if (expectedOptionCount > 1)
        {
            Assert.Equal(expectedOption2Value, parsed.Options[expectedOption2Name]);
        }
    }

    [Fact]
    public void ParsesAnEmptyArgumentList()
    {
        var parsed = Options.Parse([], FlagNames, SwitchNames);

        Assert.Empty(parsed.Positionals);
        Assert.Empty(parsed.Options);
    }

    [Theory]
    [InlineData(new[] { "--x" }, "unknown option --x")]
    [InlineData(new[] { "--map" }, "option --map needs a value")]
    [InlineData(new[] { "--map", "--strict" }, "option --map needs a value")]
    [InlineData(new[] { "--map=a", "--map=b" }, "option --map given twice")]
    [InlineData(new[] { "--strict", "--strict" }, "option --strict given twice")]
    [InlineData(new[] { "--strict=1" }, "option --strict takes no value")]
    [InlineData(new[] { "--map=a", "--map" }, "option --map given twice")]
    public void RejectsInvalidOptions(string[] args, string expectedMessage)
    {
        var exception = Assert.Throws<FailureException>(() => Options.Parse(args, FlagNames, SwitchNames));

        Assert.Equal(2, exception.Code);
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void ExpectAcceptsAnExactPositionalCount()
    {
        var parsed = new Parsed();
        parsed.Positionals.Add("a");

        parsed.Expect(1, "scan FILE");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ExpectRejectsAMismatchedPositionalCount(int count)
    {
        var parsed = new Parsed();
        parsed.Positionals.Add("a");

        var exception = Assert.Throws<FailureException>(() => parsed.Expect(count, "scan FILE"));

        Assert.Equal(2, exception.Code);
        Assert.Equal("usage: maskback scan FILE", exception.Message);
    }
}

using Maskback.Base;

namespace Maskback.Tests;

public sealed class InputsTests
{
    [Fact]
    public void ReadsFileWithItsOriginalLabel()
    {
        var fileCalls = 0;
        var stdinCalls = 0;
        var fileName = string.Empty;
        var inputs = new Inputs(
            name =>
            {
                fileCalls++;
                fileName = name;
                return [0x61];
            },
            () =>
            {
                stdinCalls++;
                return [];
            });

        var result = inputs.Read("in.txt");

        Assert.Equal(("in.txt", "a"), result);
        Assert.Equal(1, fileCalls);
        Assert.Equal("in.txt", fileName);
        Assert.Equal(0, stdinCalls);
    }

    [Fact]
    public void ReadsStandardInputAndRemovesItsBom()
    {
        var fileCalls = 0;
        var stdinCalls = 0;
        var inputs = new Inputs(
            _ =>
            {
                fileCalls++;
                return null;
            },
            () =>
            {
                stdinCalls++;
                return [0xEF, 0xBB, 0xBF, 0x61];
            });

        var result = inputs.Read("-");

        Assert.Equal(("<stdin>", "a"), result);
        Assert.Equal(0, fileCalls);
        Assert.Equal(1, stdinCalls);
    }

    [Fact]
    public void ReportsAnUnreadableFile()
    {
        var fileCalls = 0;
        var stdinCalls = 0;
        var fileName = string.Empty;
        var inputs = new Inputs(
            name =>
            {
                fileCalls++;
                fileName = name;
                return null;
            },
            () =>
            {
                stdinCalls++;
                return [];
            });

        var exception = Assert.Throws<FailureException>(() => inputs.Read("missing.txt"));

        Assert.Equal(2, exception.Code);
        Assert.Equal("cannot read missing.txt", exception.Message);
        Assert.Equal(1, fileCalls);
        Assert.Equal("missing.txt", fileName);
        Assert.Equal(0, stdinCalls);
    }

    [Fact]
    public void ReportsInvalidStandardInputAsUtf8Failure()
    {
        var fileCalls = 0;
        var stdinCalls = 0;
        var inputs = new Inputs(
            _ =>
            {
                fileCalls++;
                return null;
            },
            () =>
            {
                stdinCalls++;
                return [0xFF];
            });

        var exception = Assert.Throws<FailureException>(() => inputs.Read("-"));

        Assert.Equal(2, exception.Code);
        Assert.Equal("<stdin>: input is not valid UTF-8", exception.Message);
        Assert.Equal(0, fileCalls);
        Assert.Equal(1, stdinCalls);
    }
}

using System.Text;
using Maskback.Base;
using Maskback.Cmd;

namespace Maskback.Tests;

public sealed class CliTests
{
    [Theory]
    [InlineData(new string[] { }, "2||" +
        "usage: maskback mask FILE [--kinds LIST] [--map FILE]\n" +
        "       maskback scan FILE [--kinds LIST] [--summary]\n" +
        "       maskback unmask FILE --map FILE [--strict]\n" +
        "       maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]\n" +
        "kinds: email, key, iban, card, ipv4\n")]
    [InlineData(new[] { "help" }, "0|" +
        "usage: maskback mask FILE [--kinds LIST] [--map FILE]\n" +
        "       maskback scan FILE [--kinds LIST] [--summary]\n" +
        "       maskback unmask FILE --map FILE [--strict]\n" +
        "       maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]\n" +
        "kinds: email, key, iban, card, ipv4\n|")]
    [InlineData(new[] { "--help" }, "0|" +
        "usage: maskback mask FILE [--kinds LIST] [--map FILE]\n" +
        "       maskback scan FILE [--kinds LIST] [--summary]\n" +
        "       maskback unmask FILE --map FILE [--strict]\n" +
        "       maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]\n" +
        "kinds: email, key, iban, card, ipv4\n|")]
    [InlineData(new[] { "--help", "ignored" }, "0|" +
        "usage: maskback mask FILE [--kinds LIST] [--map FILE]\n" +
        "       maskback scan FILE [--kinds LIST] [--summary]\n" +
        "       maskback unmask FILE --map FILE [--strict]\n" +
        "       maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]\n" +
        "kinds: email, key, iban, card, ipv4\n|")]
    [InlineData(new[] { "--version" }, "0|maskback 0.1.0\n|")]
    [InlineData(new[] { "--version", "ignored" }, "0|maskback 0.1.0\n|")]
    [InlineData(new[] { "bogus" }, "2||maskback: unknown command bogus\n")]
    [InlineData(new[] { "--x" }, "2||maskback: unknown command --x\n")]
    public void DispatchesHelpVersionAndUnknownCommands(string[] args, string expected)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        List<string> reads = [];
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null;
            },
            () => []);

        var output = new Output();
        var code = Cli.Run(args, inputs, output);

        Assert.Equal(expected, $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void DispatchesMaskAndCollectsMap()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["in.txt"] = "a@b.co"
        };
        List<string> reads = [];
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "in.txt", "--map", "map.txt"], inputs, output);

        Assert.Equal("0|{{EMAIL_1}}|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(["in.txt"], reads);
        Assert.Equal(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["map.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n"
            },
            output.Files);
    }

    [Fact]
    public void DispatchesScanAndReportsFinding()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["in.txt"] = "a@b.co"
        };
        List<string> reads = [];
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt"], inputs, output);

        Assert.Equal("1|1:1 email a@b.co\nmaskback: 1 findings\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(["in.txt"], reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void DispatchesUnmaskAndRestoresReply()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["map.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            ["reply.txt"] = "Hi {{EMAIL_1}}"
        };
        List<string> reads = [];
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["unmask", "reply.txt", "--map", "map.txt"], inputs, output);

        Assert.Equal("0|Hi a@b.co|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(["map.txt", "reply.txt"], reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void DispatchesRelayAndCollectsSentLog()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["prompt.txt"] = "Email a@b.co",
            ["script.txt"] = "Done {{EMAIL_1}}"
        };
        List<string> reads = [];
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(
            ["relay", "prompt.txt", "--script", "script.txt", "--sent", "sent.txt"],
            inputs,
            output);

        Assert.Equal("0|Done a@b.co\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(["script.txt", "prompt.txt"], reads);
        Assert.Equal(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["sent.txt"] = "call 1\nuser Email {{EMAIL_1}}\n"
            },
            output.Files);
    }
}

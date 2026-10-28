using System.Text;
using Maskback.Base;
using Maskback.Cmd;

namespace Maskback.Tests;

public sealed class RelayCommandTests
{
    // This suite contains 11 literal cases.
    [Fact]
    public void RejectsMissingPromptBeforeReadingInputs()
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

        var code = Cli.Run(["relay"], inputs, output);

        Assert.Equal(
            "2||maskback: usage: maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RequiresScriptForPromptBeforeReading()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p.txt"] = "a@b.co"
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

        var code = Cli.Run(["relay", "p.txt"], inputs, output);

        Assert.Equal("2||maskback: option --script is required\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RejectsInvalidKindsAfterPositionalValidation()
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

        var code = Cli.Run(["relay", "--kinds", "phone"], inputs, output);

        Assert.Equal(
            "2||maskback: usage: maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RequiresScriptBeforeValidatingKinds()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p.txt"] = "a@b.co"
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

        var code = Cli.Run(["relay", "p.txt", "--kinds", "phone"], inputs, output);

        Assert.Equal("2||maskback: option --script is required\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RejectsInvalidKindsBeforeReadingScript()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["s.txt"] = "one reply"
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

        var code = Cli.Run(["relay", "p.txt", "--script", "s.txt", "--kinds", "phone"], inputs, output);

        Assert.Equal("2||maskback: unknown kind phone\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ParsesEmptyScriptBeforeReadingPrompts()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["s.txt"] = string.Empty,
            ["p.txt"] = "a@b.co"
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

        var code = Cli.Run(["relay", "p.txt", "--script", "s.txt", "--sent", "log.txt"], inputs, output);

        Assert.Equal("2||maskback: s.txt: reply 1 is empty\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Single(reads);
        Assert.Equal("s.txt", reads[0]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ReadsAllPromptsBeforeRunningProvider()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["s.txt"] = "one reply",
            ["p0.txt"] = "a@b.co"
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
            ["relay", "p0.txt", "missing.txt", "--script", "s.txt", "--sent", "log.txt"],
            inputs,
            output);

        Assert.Equal("2||maskback: cannot read missing.txt\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(3, reads.Count);
        Assert.Equal("s.txt", reads[0]);
        Assert.Equal("p0.txt", reads[1]);
        Assert.Equal("missing.txt", reads[2]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void PreservesSharedIdentityAndWritesCompletePrivateLog()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p0.txt"] = "a@b.co",
            ["p1.txt"] = "a@b.co",
            ["s.txt"] = "{{EMAIL_1}}\n---\n{{EMAIL_1}}\n"
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
            ["relay", "p0.txt", "p1.txt", "--script", "s.txt", "--sent", "log.txt"],
            inputs,
            output);

        Assert.Equal("0|a@b.co\n---\na@b.co\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Single(output.Files);
        Assert.Equal(
            "call 1\nuser {{EMAIL_1}}\ncall 2\nuser {{EMAIL_1}}\nassistant {{EMAIL_1}}\\n\nuser {{EMAIL_1}}\n",
            output.Files["log.txt"]);
        Assert.Equal(3, reads.Count);
        Assert.Equal("s.txt", reads[0]);
        Assert.Equal("p0.txt", reads[1]);
        Assert.Equal("p1.txt", reads[2]);
    }

    [Fact]
    public void KeepsExhaustionAtomicWhenSentLogWasRequested()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p.txt"] = "a@b.co",
            ["s.txt"] =
                "one reply\n---\none reply\n---\none reply\n---\none reply\n---\none reply\n" +
                "---\none reply\n---\none reply\n---\none reply\n---\none reply\n---\none reply"
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
            [
                "relay", "p.txt", "p.txt", "p.txt", "p.txt", "p.txt", "p.txt",
                "p.txt", "p.txt", "p.txt", "p.txt", "p.txt",
                "--script", "s.txt", "--sent", "log.txt"
            ],
            inputs,
            output);

        Assert.Equal("1||maskback: script has no reply for call 11\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(12, reads.Count);
        Assert.Equal("s.txt", reads[0]);
        Assert.Equal("p.txt", reads[1]);
        Assert.Equal("p.txt", reads[11]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void FiltersKindsAndWarnsAboutUnknownPlaceholder()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p0.txt"] = "a@b.co 1.2.3.4",
            ["s.txt"] = "{{IPV4_1}} {{EMAIL_9}}\n"
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
        var promptDescription = $"p0.txt=\"{files["p0.txt"]}\"";
        Assert.Equal("p0.txt=\"a@b.co 1.2.3.4\"", promptDescription);

        var code = Cli.Run(
            ["relay", "p0.txt", "--script", "s.txt", "--kinds", "ipv4"],
            inputs,
            output);

        Assert.Equal(
            "0|1.2.3.4 {{EMAIL_9}}\n|maskback: unknown placeholder {{EMAIL_9}}\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("s.txt", reads[0]);
        Assert.Equal("p0.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RunsSuppliedWorkedRelayExampleWithCompletePrivateLog()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["p0.txt"] = "Please email alice@example.com about card 4111-1111-1111-1111.\n",
            ["p1.txt"] = "Also tell bob@mail.example.org and alice@example.com.\n",
            ["s.txt"] =
                "Sure, I will write to {{EMAIL_1}}.\n" +
                "---\n" +
                "Done: {{EMAIL_2}} and {{EMAIL_1}} know; {{CARD_1}} is noted; {{EMAIL_3}}?\n"
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
            ["relay", "p0.txt", "p1.txt", "--script", "s.txt", "--sent", "log.txt"],
            inputs,
            output);

        Assert.Equal(
            "0|Sure, I will write to alice@example.com.\n---\nDone: bob@mail.example.org and alice@example.com know; 4111-1111-1111-1111 is noted; {{EMAIL_3}}?\n|maskback: unknown placeholder {{EMAIL_3}}\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Single(output.Files);
        Assert.Equal(
            "call 1\nuser Please email {{EMAIL_1}} about card {{CARD_1}}.\\n\ncall 2\nuser Please email {{EMAIL_1}} about card {{CARD_1}}.\\n\nassistant Sure, I will write to {{EMAIL_1}}.\\n\nuser Also tell {{EMAIL_2}} and {{EMAIL_1}}.\\n\n",
            output.Files["log.txt"]);
        Assert.Equal(3, reads.Count);
        Assert.Equal("s.txt", reads[0]);
        Assert.Equal("p0.txt", reads[1]);
        Assert.Equal("p1.txt", reads[2]);
    }
}

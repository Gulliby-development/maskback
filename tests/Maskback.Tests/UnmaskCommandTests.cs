using System.Text;
using Maskback.Base;
using Maskback.Cmd;

namespace Maskback.Tests;

public sealed class UnmaskCommandTests
{
    // This suite contains 13 literal cases, counting each theory row.
    [Fact]
    public void RejectsSwitchValuesBeforeReadingFiles()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            ["r.txt"] = "{{EMAIL_1}}"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt", "--strict=1"], inputs, output);

        Assert.Equal("2||maskback: option --strict takes no value\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Theory]
    [InlineData(new[] { "unmask" }, "2||maskback: usage: maskback unmask FILE --map FILE [--strict]\n")]
    [InlineData(new[] { "unmask", "--map", "m.txt" }, "2||maskback: usage: maskback unmask FILE --map FILE [--strict]\n")]
    public void ValidatesPositionalsBeforeReadingMap(string[] args, string expected)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n"
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

        var code = Cli.Run(args, inputs, output);

        Assert.Equal(expected, $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RequiresMapAfterValidatingPositionals()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["r.txt"] = "{{EMAIL_1}}"
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

        var code = Cli.Run(["unmask", "r.txt"], inputs, output);

        Assert.Equal("2||maskback: option --map is required\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ParsesMapBeforeReadingMissingReply()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "bad"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt"], inputs, output);

        Assert.Equal("2||maskback: m.txt: not a maskback map\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Single(reads);
        Assert.Equal("m.txt", reads[0]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RestoresKnownPlaceholder()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            ["r.txt"] = "Hi {{EMAIL_1}}"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt"], inputs, output);

        Assert.Equal("0|Hi a@b.co|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void StrictModeSucceedsForKnownPlaceholder()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            ["r.txt"] = "{{EMAIL_1}}"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt", "--strict"], inputs, output);

        Assert.Equal("0|a@b.co|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void WarnsOnceForEachDistinctUnknownInEncounterOrder()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            ["r.txt"] = "{{EMAIL_7}} {{EMAIL_7}} {{KEY_1}}"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt"], inputs, output);

        Assert.Equal(
            "0|{{EMAIL_7}} {{EMAIL_7}} {{KEY_1}}|maskback: unknown placeholder {{EMAIL_7}}\nmaskback: unknown placeholder {{KEY_1}}\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void StrictModeReturnsOneForUnknownPlaceholder()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            ["r.txt"] = "{{EMAIL_7}}"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt", "--strict"], inputs, output);

        Assert.Equal(
            "1|{{EMAIL_7}}|maskback: unknown placeholder {{EMAIL_7}}\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void LeavesMalformedPlaceholderFormsUntouchedWithoutWarnings()
    {
        var malformed = "{{email_1}} {{EMAIL_01}} {{ EMAIL_1 }} {{EMAIL_1} {{EMAIL_1234567890}}";
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            ["r.txt"] = malformed
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt", "--strict"], inputs, output);

        Assert.Equal("0|{{email_1}} {{EMAIL_01}} {{ EMAIL_1 }} {{EMAIL_1} {{EMAIL_1234567890}}|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RestoresSuppliedWorkedExample()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] =
                "maskback-map 1\n" +
                "{{EMAIL_1}}\talice@example.com\n" +
                "{{EMAIL_2}}\tbob@mail.example.org\n" +
                "{{CARD_1}}\t4111-1111-1111-1111\n" +
                "{{IBAN_1}}\tGB82WEST12345698765432\n" +
                "{{IPV4_1}}\t192.168.1.10\n" +
                "{{KEY_1}}\tsk-abcdefghijklmnopqrstuvwxyz\n" +
                "{{KEY_2}}\tAKIAABCDEFGHIJKLMNOP\n",
            ["in.txt"] =
                            "Mail alice@example.com or (bob@mail.example.org).\n" +
                            "Card 4111-1111-1111-1111, pay GB82WEST12345698765432 from 192.168.1.10.\n" +
                            "key sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again alice@example.com\n",
            ["r.txt"] = "Dear {{EMAIL_1}}, your card {{CARD_1}} is fine; {{EMAIL_7}} is not known.\n"
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

        var scanInputs = new Inputs(
            name => files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null,
            () => []);
        var scanOutput = new Output();
        var scanCode = Cli.Run(["scan", "in.txt"], scanInputs, scanOutput);

        Assert.Equal(
            "1|1:6 email alice@example.com\n1:28 email bob@mail.example.org\n2:6 card 4111-1111-1111-1111\n2:31 iban GB82WEST12345698765432\n2:59 ipv4 192.168.1.10\n3:5 key sk-abcdefghijklmnopqrstuvwxyz\n3:39 key AKIAABCDEFGHIJKLMNOP\n3:67 email alice@example.com\nmaskback: 8 findings\n|",
            $"{scanCode}|{scanOutput.Out}|{scanOutput.Err}");
        Assert.Empty(scanOutput.Files);

        var maskInputs = new Inputs(
            name => files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null,
            () => []);
        var maskOutput = new Output();
        var maskCode = Cli.Run(["mask", "in.txt", "--map", "m.txt"], maskInputs, maskOutput);

        Assert.Equal(
            "0|Mail {{EMAIL_1}} or ({{EMAIL_2}}).\nCard {{CARD_1}}, pay {{IBAN_1}} from {{IPV4_1}}.\nkey {{KEY_1}} and {{KEY_2}}, again {{EMAIL_1}}\n|",
            $"{maskCode}|{maskOutput.Out}|{maskOutput.Err}");
        Assert.Single(maskOutput.Files);
        Assert.Equal(
            "maskback-map 1\n{{EMAIL_1}}\talice@example.com\n{{EMAIL_2}}\tbob@mail.example.org\n{{CARD_1}}\t4111-1111-1111-1111\n{{IBAN_1}}\tGB82WEST12345698765432\n{{IPV4_1}}\t192.168.1.10\n{{KEY_1}}\tsk-abcdefghijklmnopqrstuvwxyz\n{{KEY_2}}\tAKIAABCDEFGHIJKLMNOP\n",
            maskOutput.Files["m.txt"]);

        var filteredMaskInputs = new Inputs(
            name => files.TryGetValue(name, out var text) ? Encoding.UTF8.GetBytes(text) : null,
            () => []);
        var filteredMaskOutput = new Output();
        var filteredMaskCode = Cli.Run(
            ["mask", "in.txt", "--kinds", "email,ipv4"],
            filteredMaskInputs,
            filteredMaskOutput);

        Assert.Equal(
            "0|Mail {{EMAIL_1}} or ({{EMAIL_2}}).\nCard 4111-1111-1111-1111, pay GB82WEST12345698765432 from {{IPV4_1}}.\nkey sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again {{EMAIL_1}}\n|",
            $"{filteredMaskCode}|{filteredMaskOutput.Out}|{filteredMaskOutput.Err}");
        Assert.Empty(filteredMaskOutput.Files);

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt"], inputs, output);

        Assert.Equal(
            "0|Dear alice@example.com, your card 4111-1111-1111-1111 is fine; {{EMAIL_7}} is not known.\n|maskback: unknown placeholder {{EMAIL_7}}\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RestoresWorkedExampleInStrictMode()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] =
                "maskback-map 1\n" +
                "{{EMAIL_1}}\talice@example.com\n" +
                "{{EMAIL_2}}\tbob@mail.example.org\n" +
                "{{CARD_1}}\t4111-1111-1111-1111\n" +
                "{{IBAN_1}}\tGB82WEST12345698765432\n" +
                "{{IPV4_1}}\t192.168.1.10\n" +
                "{{KEY_1}}\tsk-abcdefghijklmnopqrstuvwxyz\n" +
                "{{KEY_2}}\tAKIAABCDEFGHIJKLMNOP\n",
            ["r.txt"] = "Dear {{EMAIL_1}}, your card {{CARD_1}} is fine; {{EMAIL_7}} is not known.\n"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt", "--strict"], inputs, output);

        Assert.Equal(
            "1|Dear alice@example.com, your card 4111-1111-1111-1111 is fine; {{EMAIL_7}} is not known.\n|maskback: unknown placeholder {{EMAIL_7}}\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ReadsValidMapThenReportsMissingReply()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["m.txt"] = "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n"
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

        var code = Cli.Run(["unmask", "r.txt", "--map", "m.txt"], inputs, output);

        Assert.Equal("2||maskback: cannot read r.txt\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(2, reads.Count);
        Assert.Equal("m.txt", reads[0]);
        Assert.Equal("r.txt", reads[1]);
        Assert.Empty(output.Files);
    }
}

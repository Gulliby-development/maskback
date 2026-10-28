using System.Text;
using Maskback.Base;
using Maskback.Cmd;

namespace Maskback.Tests;

public sealed class ScanCommandTests
{
    [Fact]
    public void RejectsExcessPositionalsBeforeReading()
    {
        var reads = new List<string>();
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("a@b.co")
        };
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt", "extra"], inputs, output);

        Assert.Equal("2||maskback: usage: maskback scan FILE [--kinds LIST]\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RejectsEmptyKindsBeforeReading()
    {
        var reads = new List<string>();
        var files = new Dictionary<string, byte[]>
        {
            ["missing.txt"] = Encoding.UTF8.GetBytes("plain")
        };
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "missing.txt", "--kinds", ""], inputs, output);

        Assert.Equal("2||maskback: unknown kind \n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RejectsTheSpecifiedInvalidStandardInputBytes()
    {
        var files = new Dictionary<string, byte[]>();
        var fileReads = new List<string>();
        var stdinReads = 0;
        var inputs = new Inputs(
            name =>
            {
                fileReads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () =>
            {
                stdinReads++;
                return [0x61, 0x40, 0x62, 0x2E, 0x63, 0x6F, 0x0A, 0xFF, 0x0A];
            });
        var output = new Output();

        var code = Cli.Run(["scan", "-"], inputs, output);

        Assert.Equal("2||maskback: <stdin>: input is not valid UTF-8\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(fileReads);
        Assert.Equal(1, stdinReads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ReportsZeroFindingsForPlainText()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("plain")
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt"], inputs, output);

        Assert.Equal("0|maskback: 0 findings\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
    }

    [Fact]
    public void CountsSupplementaryUnicodeAndCrInUtf16Columns()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("😀 a@b.co\r\nx\r a@b.co")
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt"], inputs, output);

        Assert.Equal("1|1:4 email a@b.co\n2:4 email a@b.co\nmaskback: 2 findings\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
    }

    [Fact]
    public void FiltersFindingsByKind()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("a@b.co 1.2.3.4")
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt", "--kinds", "ipv4"], inputs, output);

        Assert.Equal("1|1:8 ipv4 1.2.3.4\nmaskback: 1 findings\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ReportsWorkedScanAndCrLfMaskExamples()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes(
                "Mail alice@example.com or (bob@mail.example.org).\n" +
                "Card 4111-1111-1111-1111, pay GB82WEST12345698765432 from 192.168.1.10.\n" +
                "key sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again alice@example.com\n")
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt"], inputs, output);

        Assert.Equal(
            "1|1:6 email alice@example.com\n" +
            "1:28 email bob@mail.example.org\n" +
            "2:6 card 4111-1111-1111-1111\n" +
            "2:31 iban GB82WEST12345698765432\n" +
            "2:59 ipv4 192.168.1.10\n" +
            "3:5 key sk-abcdefghijklmnopqrstuvwxyz\n" +
            "3:39 key AKIAABCDEFGHIJKLMNOP\n" +
            "3:67 email alice@example.com\n" +
            "maskback: 8 findings\n|",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);

        var maskFiles = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("(a@b.co).\r\na@b.co")
        };
        var maskInputs = new Inputs(
            name => maskFiles.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var maskOutput = new Output();

        var maskCode = Cli.Run(["mask", "in.txt", "--map", "m.txt"], maskInputs, maskOutput);

        Assert.Equal("0|({{EMAIL_1}}).\r\n{{EMAIL_1}}|", $"{maskCode}|{maskOutput.Out}|{maskOutput.Err}");
        Assert.Single(maskOutput.Files);
        Assert.Equal("maskback-map 1\n{{EMAIL_1}}\ta@b.co\n", maskOutput.Files["m.txt"]);
    }

    [Fact]
    public void ReportsZeroFindingsForEmptyInput()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("")
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt"], inputs, output);

        Assert.Equal("0|maskback: 0 findings\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
    }

    [Fact]
    public void CountsLoneCarAsAColumnCharacter()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("x\ra@b.co")
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt"], inputs, output);

        Assert.Equal("1|1:3 email a@b.co\nmaskback: 1 findings\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
    }

    [Fact]
    public void DoesNotDuplicateFindingsForRepeatedKinds()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("a@b.co a@b.co")
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", "in.txt", "--kinds", "email,email"], inputs, output);

        Assert.Equal("1|1:1 email a@b.co\n1:8 email a@b.co\nmaskback: 2 findings\n|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
    }
}

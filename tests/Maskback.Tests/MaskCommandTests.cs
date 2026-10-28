using System.Text;
using Maskback.Base;
using Maskback.Cmd;

namespace Maskback.Tests;

public sealed class MaskCommandTests
{
    [Theory]
    [InlineData(new[] { "mask", "in.txt", "--kinds=email" }, "in.txt", "a@b.co", "0|{{EMAIL_1}}|")]
    [InlineData(new[] { "mask", "--", "--x" }, "--x", "plain", "0|plain|")]
    public void ParsesEqualsValuesAndOptionTermination(string[] args, string fileName, string fileText, string expected)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [fileName] = Encoding.UTF8.GetBytes(fileText)
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(args, inputs, output);

        Assert.Equal(expected, $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
        Assert.Equal([fileName], reads);
    }

    [Theory]
    [InlineData(new[] { "mask", "in.txt", "--x" }, "2||maskback: unknown option --x\n")]
    [InlineData(new[] { "mask", "in.txt", "--map" }, "2||maskback: option --map needs a value\n")]
    [InlineData(new[] { "mask", "in.txt", "--map", "m", "--map", "n" }, "2||maskback: option --map given twice\n")]
    public void RejectsInvalidOptionsBeforeReading(string[] args, string expected)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("a@b.co")
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(args, inputs, output);

        Assert.Equal(expected, $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RejectsMissingPositionalBeforeReading()
    {
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask"], inputs, output);

        Assert.Equal(
            "2||maskback: usage: maskback mask FILE [--kinds LIST] [--map FILE]\n",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ValidatesKindsBeforeReading()
    {
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "missing.txt", "--kinds", "phone", "--map", "m.txt"], inputs, output);

        Assert.Equal("2||maskback: unknown kind phone\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ReportsMissingInput()
    {
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "missing.txt"], inputs, output);

        Assert.Equal("2||maskback: cannot read missing.txt\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(["missing.txt"], reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RejectsInvalidFileBytesWithoutWritingMap()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["bad.txt"] = [0xFF]
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "bad.txt", "--map", "m.txt"], inputs, output);

        Assert.Equal("2||maskback: bad.txt: input is not valid UTF-8\n", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(["bad.txt"], reads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void RemovesBomFromStandardInputBeforeMasking()
    {
        var fileReads = new List<string>();
        var stdinReads = 0;
        var inputs = new Inputs(
            name =>
            {
                fileReads.Add(name);
                return null;
            },
            () =>
            {
                stdinReads++;
                return [0xEF, 0xBB, 0xBF, 0x61, 0x40, 0x62, 0x2E, 0x63, 0x6F];
            });
        var output = new Output();

        var code = Cli.Run(["mask", "-"], inputs, output);

        Assert.Equal("0|{{EMAIL_1}}|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(fileReads);
        Assert.Equal(1, stdinReads);
        Assert.Empty(output.Files);
    }

    [Fact]
    public void PreservesCrLfAndReusesPlaceholderForRepeatedEmail()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("(a@b.co).\r\na@b.co")
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "in.txt", "--map", "m.txt"], inputs, output);

        Assert.Equal("0|({{EMAIL_1}}).\r\n{{EMAIL_1}}|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(
            "maskback-map 1\n{{EMAIL_1}}\ta@b.co\n",
            output.Files["m.txt"]);
        Assert.Single(output.Files);
        Assert.Equal(["in.txt"], reads);
    }

    [Fact]
    public void PreservesPlainTextWithoutFinalNewline()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("plain")
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "in.txt"], inputs, output);

        Assert.Equal("0|plain|", $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
        Assert.Equal(["in.txt"], reads);
    }

    [Fact]
    public void ProducesSuppliedWorkedMaskAndMap()
    {
        var inputText =
            "Mail alice@example.com or (bob@mail.example.org).\n" +
            "Card 4111-1111-1111-1111, pay GB82WEST12345698765432 from 192.168.1.10.\n" +
            "key sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again alice@example.com\n";
        var mapText =
            "maskback-map 1\n{{EMAIL_1}}\talice@example.com\n{{EMAIL_2}}\tbob@mail.example.org\n{{CARD_1}}\t4111-1111-1111-1111\n{{IBAN_1}}\tGB82WEST12345698765432\n{{IPV4_1}}\t192.168.1.10\n{{KEY_1}}\tsk-abcdefghijklmnopqrstuvwxyz\n{{KEY_2}}\tAKIAABCDEFGHIJKLMNOP\n";
        var replyText = "Dear {{EMAIL_1}}, your card {{CARD_1}} is fine; {{EMAIL_7}} is not known.\n";
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["in.txt"] = Encoding.UTF8.GetBytes(inputText),
            ["m.txt"] = Encoding.UTF8.GetBytes(mapText),
            ["r.txt"] = Encoding.UTF8.GetBytes(replyText)
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);

        var scanOutput = new Output();
        var scanCode = Cli.Run(["scan", "in.txt"], inputs, scanOutput);
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
            $"{scanCode}|{scanOutput.Out}|{scanOutput.Err}");
        Assert.Empty(scanOutput.Files);
        Assert.Equal(["in.txt"], reads);

        var output = new Output();
        var code = Cli.Run(["mask", "in.txt", "--map", "m.txt"], inputs, output);
        Assert.Equal(
            "0|Mail {{EMAIL_1}} or ({{EMAIL_2}}).\n" +
            "Card {{CARD_1}}, pay {{IBAN_1}} from {{IPV4_1}}.\n" +
            "key {{KEY_1}} and {{KEY_2}}, again {{EMAIL_1}}\n|",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Equal(mapText, output.Files["m.txt"]);
        Assert.Single(output.Files);
        Assert.Equal(["in.txt", "in.txt"], reads);

        var unmaskOutput = new Output();
        var unmaskCode = Cli.Run(["unmask", "r.txt", "--map", "m.txt"], inputs, unmaskOutput);
        Assert.Equal(
            "0|Dear alice@example.com, your card 4111-1111-1111-1111 is fine; {{EMAIL_7}} is not known.\n|" +
            "maskback: unknown placeholder {{EMAIL_7}}\n",
            $"{unmaskCode}|{unmaskOutput.Out}|{unmaskOutput.Err}");
        Assert.Empty(unmaskOutput.Files);
        Assert.Equal(["in.txt", "in.txt", "m.txt", "r.txt"], reads);
    }

    [Fact]
    public void FiltersWorkedMaskToEmailAndIpv4()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["in.txt"] = Encoding.UTF8.GetBytes(
                "Mail alice@example.com or (bob@mail.example.org).\n" +
                "Card 4111-1111-1111-1111, pay GB82WEST12345698765432 from 192.168.1.10.\n" +
                "key sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again alice@example.com\n")
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "in.txt", "--kinds", "email,ipv4"], inputs, output);

        Assert.Equal(
            "0|Mail {{EMAIL_1}} or ({{EMAIL_2}}).\n" +
            "Card 4111-1111-1111-1111, pay GB82WEST12345698765432 from {{IPV4_1}}.\n" +
            "key sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again {{EMAIL_1}}\n|",
            $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
        Assert.Equal(["in.txt"], reads);
    }

    [Fact]
    public void AcceptsRepeatedKindsWithoutDuplicatingFindings()
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["in.txt"] = Encoding.UTF8.GetBytes("a@b.co")
        };
        var reads = new List<string>();
        var inputs = new Inputs(
            name =>
            {
                reads.Add(name);
                return files.TryGetValue(name, out var bytes) ? bytes : null;
            },
            () => []);
        var output = new Output();

        var code = Cli.Run(["mask", "in.txt", "--kinds", "email,email", "--map", "m.txt"], inputs, output);

        Assert.Equal("0|{{EMAIL_1}}|", $"{code}|{output.Out}|{output.Err}");
        Assert.Equal("maskback-map 1\n{{EMAIL_1}}\ta@b.co\n", output.Files["m.txt"]);
        Assert.Single(output.Files);
        Assert.Equal(["in.txt"], reads);
    }
}

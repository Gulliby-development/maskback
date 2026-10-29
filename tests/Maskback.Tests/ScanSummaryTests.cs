using System.Text;
using Maskback.Base;
using Maskback.Cmd;

namespace Maskback.Tests;

public sealed class ScanSummaryTests
{
    private const string AllKinds =
        "Mail alice@example.com or (bob@mail.example.org).\n" +
        "Card 4111-1111-1111-1111, pay GB82WEST12345698765432 from 192.168.1.10.\n" +
        "key sk-abcdefghijklmnopqrstuvwxyz and AKIAABCDEFGHIJKLMNOP, again alice@example.com\n";

    [Theory]
    [InlineData("plain", new[] { "in.txt", "--summary" }, "0|email 0\nkey 0\niban 0\ncard 0\nipv4 0\nmaskback: 0 findings\n|")]
    [InlineData("", new[] { "in.txt", "--summary" }, "0|email 0\nkey 0\niban 0\ncard 0\nipv4 0\nmaskback: 0 findings\n|")]
    [InlineData("a@b.co 1.2.3.4 a@b.co", new[] { "in.txt", "--summary" }, "1|email 2\nkey 0\niban 0\ncard 0\nipv4 1\nmaskback: 3 findings\n|")]
    [InlineData(AllKinds, new[] { "in.txt", "--summary" }, "1|email 3\nkey 2\niban 1\ncard 1\nipv4 1\nmaskback: 8 findings\n|")]
    [InlineData(AllKinds, new[] { "--summary", "in.txt" }, "1|email 3\nkey 2\niban 1\ncard 1\nipv4 1\nmaskback: 8 findings\n|")]
    [InlineData(AllKinds, new[] { "--summary", "--", "in.txt" }, "1|email 3\nkey 2\niban 1\ncard 1\nipv4 1\nmaskback: 8 findings\n|")]
    [InlineData("a@b.co 1.2.3.4", new[] { "in.txt", "--summary", "--kinds", "ipv4" }, "1|ipv4 1\nmaskback: 1 findings\n|")]
    [InlineData("a@b.co 1.2.3.4", new[] { "in.txt", "--kinds=ipv4,email", "--summary" }, "1|email 1\nipv4 1\nmaskback: 2 findings\n|")]
    [InlineData("a@b.co", new[] { "in.txt", "--kinds", "email,email", "--summary" }, "1|email 1\nmaskback: 1 findings\n|")]
    [InlineData("a@b.co", new[] { "in.txt", "--kinds", "ipv4", "--summary" }, "0|ipv4 0\nmaskback: 0 findings\n|")]
    [InlineData("a@b.co", new[] { "in.txt", "--summary=1" }, "2||maskback: option --summary takes no value\n")]
    [InlineData("a@b.co", new[] { "in.txt", "--summary", "--summary" }, "2||maskback: option --summary given twice\n")]
    [InlineData("a@b.co", new[] { "in.txt", "--summary", "--kinds", "phone" }, "2||maskback: unknown kind phone\n")]
    [InlineData("a@b.co", new[] { "--summary" }, "2||maskback: usage: maskback scan FILE [--kinds LIST] [--summary]\n")]
    [InlineData("a@b.co", new[] { "in.txt", "--summary", "extra" }, "2||maskback: usage: maskback scan FILE [--kinds LIST] [--summary]\n")]
    public void PrintsOneCountPerSelectedKind(string text, string[] args, string expected)
    {
        var files = new Dictionary<string, byte[]>
        {
            ["in.txt"] = Encoding.UTF8.GetBytes(text)
        };
        var inputs = new Inputs(
            name => files.TryGetValue(name, out var bytes) ? bytes : null,
            () => []);
        var output = new Output();

        var code = Cli.Run(["scan", .. args], inputs, output);

        Assert.Equal(expected, $"{code}|{output.Out}|{output.Err}");
        Assert.Empty(output.Files);
    }

    [Fact]
    public void ReadsStandardInputAndKeepsTheRowsOfTheDefaultReport()
    {
        var inputs = new Inputs(
            _ => null,
            () => Encoding.UTF8.GetBytes("x 1.2.3.4\n"));
        var summary = new Output();
        var rows = new Output();

        var summaryCode = Cli.Run(["scan", "-", "--summary"], inputs, summary);
        var rowsCode = Cli.Run(["scan", "-"], inputs, rows);

        Assert.Equal("1|email 0\nkey 0\niban 0\ncard 0\nipv4 1\nmaskback: 1 findings\n|", $"{summaryCode}|{summary.Out}|{summary.Err}");
        Assert.Equal("1|1:3 ipv4 1.2.3.4\nmaskback: 1 findings\n|", $"{rowsCode}|{rows.Out}|{rows.Err}");
    }
}

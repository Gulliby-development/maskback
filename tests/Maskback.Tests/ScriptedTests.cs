using Maskback.Base;
using Maskback.Fake;

namespace Maskback.Tests;

public sealed class ScriptedTests
{
    // These methods contain 15 literal cases: eight parsing successes, three parsing errors, and four completion cases.
    [Theory]
    [InlineData("one", new string[] { "one\n" })]
    [InlineData("one\n", new string[] { "one\n" })]
    [InlineData("one\r\ntwo\r\n", new string[] { "one\ntwo\n" })]
    [InlineData("one\n---\ntwo", new string[] { "one\n", "two\n" })]
    [InlineData("one\n---\n", new string[] { "one\n" })]
    [InlineData("\n", new string[] { "\n" })]
    [InlineData(" ---\n--- ", new string[] { " ---\n--- \n" })]
    [InlineData("one\rtwo", new string[] { "one\rtwo\n" })]
    public void ParsesRepliesWithExactLineBoundaries(string text, string[] expected)
    {
        Assert.Equal(expected, Scripted.ParseScript(text, "s.txt"));
    }

    [Theory]
    [InlineData("", 2, "s.txt: reply 1 is empty")]
    [InlineData("---\none", 2, "s.txt: reply 1 is empty")]
    [InlineData("one\n---\n---\ntwo", 2, "s.txt: reply 2 is empty")]
    public void RejectsEmptyReplies(string text, int code, string message)
    {
        var exception = Assert.Throws<FailureException>(() => Scripted.ParseScript(text, "s.txt"));

        Assert.Equal(code, exception.Code);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ReturnsRepliesInOrderAndSnapshotsMutatedConversation()
    {
        var provider = new Scripted(["first", "second"]);
        var messages = new List<(string Role, string Text)> { ("user", "one") };

        Assert.Equal(["15\n"], Scripted.ParseScript("15", "s.txt"));
        Assert.Equal("first", provider.Complete(messages));

        messages.Add(("assistant", "first"));
        messages.Add(("user", "two"));
        Assert.Equal("second", provider.Complete(messages));

        messages.Add(("assistant", "second"));
        messages.Add(("user", "three"));

        Assert.Equal(2, provider.Calls.Count);
        Assert.Equal([("user", "one")], provider.Calls[0]);
        Assert.Equal([("user", "one"), ("assistant", "first"), ("user", "two")], provider.Calls[1]);
    }

    [Fact]
    public void RecordsExhaustedAttemptAfterReturningOnlyReply()
    {
        var provider = new Scripted(["only"]);
        var messages = new List<(string Role, string Text)> { ("user", "x") };

        Assert.Equal("only", provider.Complete(messages));

        var exception = Assert.Throws<FailureException>(() => provider.Complete(messages));

        Assert.Equal(1, exception.Code);
        Assert.Equal("script has no reply for call 2", exception.Message);
        Assert.Equal(2, provider.Calls.Count);
        Assert.Equal([("user", "x")], provider.Calls[0]);
        Assert.Equal([("user", "x")], provider.Calls[1]);
    }

    [Fact]
    public void RecordsEmptySnapshotWhenEmptyProviderIsExhausted()
    {
        var provider = new Scripted([]);
        var exception = Assert.Throws<FailureException>(() => provider.Complete([]));

        Assert.Equal(1, exception.Code);
        Assert.Equal("script has no reply for call 1", exception.Message);
        Assert.Single(provider.Calls);
        Assert.Equal([], provider.Calls[0]);
    }

    [Fact]
    public void ReusesProviderForIndependentConversations()
    {
        var provider = new Scripted(["a", "b", "c"]);
        var first = new List<(string Role, string Text)> { ("user", "x") };
        var second = new List<(string Role, string Text)> { ("user", "y") };
        var third = new List<(string Role, string Text)>();

        Assert.Equal("a", provider.Complete(first));
        Assert.Equal("b", provider.Complete(second));
        Assert.Equal("c", provider.Complete(third));

        Assert.Equal(3, provider.Calls.Count);
        Assert.Equal([("user", "x")], provider.Calls[0]);
        Assert.Equal([("user", "y")], provider.Calls[1]);
        Assert.Equal([], provider.Calls[2]);
    }
}

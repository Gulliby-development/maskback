using Maskback.Base;
using Maskback.Fake;
using Maskback.Model;

namespace Maskback.Tests;

public sealed class RelayTests
{
    [Fact]
    public void RelaysTwoTurnsWithoutRestoringProviderHistory()
    {
        var provider = new Scripted(
        [
            "Sure, I will write to {{EMAIL_1}}.\n",
            "Done: {{EMAIL_2}} and {{EMAIL_1}} know; {{CARD_1}} is noted; {{EMAIL_3}}?\n",
        ]);

        var (Text, Unknown, Log) = Relay.Run(
            [
                "Please email alice@example.com about card 4111-1111-1111-1111.\n",
                "Also tell bob@mail.example.org and alice@example.com.\n",
            ],
            Kinds.All,
            provider);

        Assert.Equal(
            "Sure, I will write to alice@example.com.\n---\nDone: bob@mail.example.org and alice@example.com know; 4111-1111-1111-1111 is noted; {{EMAIL_3}}?\n",
            Text);
        Assert.Equal(["{{EMAIL_3}}"], Unknown);
        Assert.Equal(
            "call 1\nuser Please email {{EMAIL_1}} about card {{CARD_1}}.\\n\ncall 2\nuser Please email {{EMAIL_1}} about card {{CARD_1}}.\\n\nassistant Sure, I will write to {{EMAIL_1}}.\\n\nuser Also tell {{EMAIL_2}} and {{EMAIL_1}}.\\n\n",
            Log);
        Assert.Equal(2, provider.Calls.Count);
        Assert.Equal([("user", "Please email {{EMAIL_1}} about card {{CARD_1}}.\n")], provider.Calls[0]);
        Assert.Equal(
            [
                ("user", "Please email {{EMAIL_1}} about card {{CARD_1}}.\n"),
                ("assistant", "Sure, I will write to {{EMAIL_1}}.\n"),
                ("user", "Also tell {{EMAIL_2}} and {{EMAIL_1}}.\n"),
            ],
            provider.Calls[1]);
        Assert.DoesNotContain("alice@example.com", Log, StringComparison.Ordinal);
        Assert.DoesNotContain("bob@mail.example.org", Log, StringComparison.Ordinal);
        Assert.DoesNotContain("4111-1111-1111-1111", Log, StringComparison.Ordinal);
        Assert.DoesNotContain("alice@example.com", provider.Calls[0][0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("bob@mail.example.org", provider.Calls[1][2].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("4111-1111-1111-1111", provider.Calls[0][0].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void RestoresOnlyMappingsAvailableOnEachTurnAndEscapesLogs()
    {
        var provider = new Scripted(
        [
            "{{EMAIL_2}} {{EMAIL_9}} {{EMAIL_2}}\\\r\n\t",
            "{{EMAIL_2}} {{EMAIL_1}} {{EMAIL_9}} {{KEY_1}}",
        ]);

        var (Text, Unknown, Log) = Relay.Run(
            ["a@b.co \\x\r\n\t1.2.3.4", "c@d.co a@b.co"],
            ["email"],
            provider);

        Assert.Equal(
            "{{EMAIL_2}} {{EMAIL_9}} {{EMAIL_2}}\\\r\n\t---\nc@d.co a@b.co {{EMAIL_9}} {{KEY_1}}",
            Text);
        Assert.Equal(["{{EMAIL_2}}", "{{EMAIL_9}}", "{{KEY_1}}"], Unknown);
        Assert.Equal(
            "call 1\nuser {{EMAIL_1}} \\\\x\\r\\n\t1.2.3.4\ncall 2\nuser {{EMAIL_1}} \\\\x\\r\\n\t1.2.3.4\nassistant {{EMAIL_2}} {{EMAIL_9}} {{EMAIL_2}}\\\\\\r\\n\t\nuser {{EMAIL_2}} {{EMAIL_1}}\n",
            Log);
        Assert.Equal([("user", "{{EMAIL_1}} \\x\r\n\t1.2.3.4")], provider.Calls[0]);
        Assert.Equal(
            [
                ("user", "{{EMAIL_1}} \\x\r\n\t1.2.3.4"),
                ("assistant", "{{EMAIL_2}} {{EMAIL_9}} {{EMAIL_2}}\\\r\n\t"),
                ("user", "{{EMAIL_2}} {{EMAIL_1}}"),
            ],
            provider.Calls[1]);
        Assert.DoesNotContain("a@b.co", Log, StringComparison.Ordinal);
        Assert.DoesNotContain("c@d.co", Log, StringComparison.Ordinal);

        Assert.DoesNotContain("a@b.co", provider.Calls[0][0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("c@d.co", provider.Calls[1][2].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void PropagatesSecondCallExhaustionWithPrivateSnapshots()
    {
        var provider = new Scripted(["{{EMAIL_1}}"]);
        var exception = Assert.Throws<FailureException>(
            () => Relay.Run(["a@b.co", "a@b.co"], ["email"], provider));

        Assert.Equal(1, exception.Code);
        Assert.Equal("script has no reply for call 2", exception.Message);
        Assert.Equal([("user", "{{EMAIL_1}}")], provider.Calls[0]);
        Assert.Equal(
            [
                ("user", "{{EMAIL_1}}"),
                ("assistant", "{{EMAIL_1}}"),
                ("user", "{{EMAIL_1}}"),
            ],
            provider.Calls[1]);
        Assert.DoesNotContain("a@b.co", provider.Calls[0][0].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("a@b.co", provider.Calls[1][2].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ReturnsEmptyResultForNoPrompts()
    {
        var provider = new Scripted([]);
        var (Text, Unknown, Log) = Relay.Run([], ["email"], provider);

        Assert.Equal("", Text);
        Assert.Equal([], Unknown);
        Assert.Equal("", Log);
        Assert.Equal([], provider.Calls);
    }
}

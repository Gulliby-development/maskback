using Maskback.Base;

namespace Maskback.Fake;

public sealed class Scripted
{
    private readonly List<string> replies;

    public Scripted(List<string> replies)
    {
        ArgumentNullException.ThrowIfNull(replies);
        this.replies = replies;
    }

    public List<List<(string Role, string Text)>> Calls { get; } = [];

    public static List<string> ParseScript(string text, string label)
    {
        var lines = Text.SplitLines(text);
        var parsedReplies = new List<string>();
        var currentLines = new List<string>();
        var replyNumber = 1;

        foreach (var line in lines)
        {
            if (!string.Equals(line, "---", StringComparison.Ordinal))
            {
                currentLines.Add(line);
                continue;
            }

            if (currentLines.Count == 0)
            {
                throw new FailureException(2, $"{label}: reply {replyNumber} is empty");
            }

            parsedReplies.Add(string.Join("\n", currentLines) + "\n");
            currentLines.Clear();
            replyNumber++;
        }

        if (currentLines.Count > 0)
        {
            parsedReplies.Add(string.Join("\n", currentLines) + "\n");
        }
        else if (parsedReplies.Count == 0)
        {
            throw new FailureException(2, $"{label}: reply {replyNumber} is empty");
        }

        return parsedReplies;
    }

    public string Complete(IReadOnlyList<(string Role, string Text)> messages)
    {
        Calls.Add([.. messages]);
        var callNumber = Calls.Count;

        if (callNumber > replies.Count)
        {
            throw new FailureException(1, $"script has no reply for call {callNumber}");
        }

        return replies[callNumber - 1];
    }
}

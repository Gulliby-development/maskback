using System.Globalization;
using System.Text;
using Maskback.Base;
using Maskback.Core;

namespace Maskback.Fake;

public static class Relay
{
    public static (string Text, List<string> Unknown, string Log) Run(
        IReadOnlyList<string> prompts,
        IReadOnlyCollection<string> kinds,
        Scripted provider)
    {
        var masker = new Masker();
        var conversation = new List<(string Role, string Text)>();
        var restoredReplies = new List<string>();
        var unknown = new List<string>();
        var seenUnknown = new HashSet<string>(StringComparer.Ordinal);

        foreach (var prompt in prompts)
        {
            var masked = masker.Mask(prompt, kinds);
            conversation.Add(("user", masked));

            var reply = provider.Complete(conversation);
            conversation.Add(("assistant", reply));

            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (placeholder, original) in masker.Table)
            {
                table.Add(placeholder, original);
            }

            var (restored, turnUnknown) = Unmasker.Unmask(reply, table);
            restoredReplies.Add(restored);

            foreach (var placeholder in turnUnknown)
            {
                if (seenUnknown.Add(placeholder))
                {
                    unknown.Add(placeholder);
                }
            }
        }

        var log = new StringBuilder();
        for (var callIndex = 0; callIndex < provider.Calls.Count; callIndex++)
        {
            log.Append("call ")
                .Append((callIndex + 1).ToString(CultureInfo.InvariantCulture))
                .Append('\n');

            foreach (var (role, text) in provider.Calls[callIndex])
            {
                log.Append(role)
                    .Append(' ')
                    .Append(Text.Escape(text))
                    .Append('\n');
            }
        }

        return (string.Join("---\n", restoredReplies), unknown, log.ToString());
    }
}

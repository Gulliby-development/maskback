using System.Text.RegularExpressions;

namespace Maskback.Core;

public static class Unmasker
{
    public static (string Text, List<string> Unknown) Unmask(string text, IReadOnlyDictionary<string, string> table)
    {
        var ordinalTable = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (placeholder, original) in table)
        {
            ordinalTable.Add(placeholder, original);
        }

        var unknown = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var restored = Placeholders.Any().Replace(text, match =>
        {
            if (ordinalTable.TryGetValue(match.Value, out var original))
            {
                return original;
            }

            if (seen.Add(match.Value))
            {
                unknown.Add(match.Value);
            }

            return match.Value;
        });

        return (restored, unknown);
    }
}

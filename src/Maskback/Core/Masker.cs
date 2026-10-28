using System.Globalization;
using System.Text;

namespace Maskback.Core;

public sealed class Masker
{
    private readonly Dictionary<(string Kind, string Original), string> placeholders = [];
    private readonly Dictionary<string, int> counters = new(StringComparer.Ordinal);

    public List<(string Placeholder, string Original)> Table { get; } = [];

    public string Placeholder(string kind, string original)
    {
        var identity = (kind, original);
        if (placeholders.TryGetValue(identity, out var existing))
        {
            return existing;
        }

        var next = counters.TryGetValue(kind, out var current) ? current + 1 : 1;
        counters[kind] = next;

        var placeholder = "{{" + kind.ToUpperInvariant() + "_" + next.ToString(CultureInfo.InvariantCulture) + "}}";
        placeholders.Add(identity, placeholder);
        Table.Add((placeholder, original));

        return placeholder;
    }

    public string Mask(string text, IReadOnlyCollection<string> kinds)
    {
        var findings = Scanner.Scan(text, kinds);
        if (findings.Count == 0)
        {
            return text;
        }

        var result = new StringBuilder(text.Length);
        var cursor = 0;
        foreach (var finding in findings)
        {
            result.Append(text.AsSpan(cursor, finding.Start - cursor));
            result.Append(Placeholder(finding.Kind, finding.Value));
            cursor = finding.Start + finding.Value.Length;
        }

        result.Append(text.AsSpan(cursor));
        return result.ToString();
    }
}

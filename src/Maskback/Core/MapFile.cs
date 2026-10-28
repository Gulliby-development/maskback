using System.Text;
using System.Text.RegularExpressions;
using Maskback.Base;

namespace Maskback.Core;

public static partial class MapFile
{
    public const string Header = "maskback-map 1";

    public static string Write(IEnumerable<(string Placeholder, string Original)> table)
    {
        var content = new StringBuilder();
        content.Append(Header).Append('\n');

        foreach (var (placeholder, original) in table)
        {
            content.Append(placeholder).Append('\t').Append(original).Append('\n');
        }

        return content.ToString();
    }

    public static Dictionary<string, string> Read(string text, string label)
    {
        var lines = Text.SplitLines(text);
        if (lines.Count == 0 || !string.Equals(lines[0], Header, StringComparison.Ordinal))
        {
            throw new FailureException(2, $"{label}: not a maskback map");
        }

        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 1; index < lines.Count; index++)
        {
            var line = lines[index];
            var separator = line.IndexOf('\t');
            if (separator < 0 || line.IndexOf('\t', separator + 1) >= 0)
            {
                throw new FailureException(2, $"{label}:{(index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}: bad map line");
            }

            var placeholder = line[..separator];
            var original = line[(separator + 1)..];
            if (!Placeholders.Whole().IsMatch(placeholder) || original.Length == 0)
            {
                throw new FailureException(2, $"{label}:{(index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}: bad map line");
            }

            if (!table.TryAdd(placeholder, original))
            {
                throw new FailureException(2, $"{label}:{(index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}: duplicate placeholder {placeholder}");
            }
        }

        return table;
    }
}

/// <summary>The placeholder syntax: two braces, capitals, underscore, a number from 1, two braces.</summary>
public static partial class Placeholders
{
    [GeneratedRegex(@"\{\{[A-Z][A-Z0-9]*_[1-9][0-9]{0,8}\}\}", RegexOptions.CultureInvariant)]
    public static partial Regex Any();

    [GeneratedRegex(@"^\{\{[A-Z][A-Z0-9]*_[1-9][0-9]{0,8}\}\}\z", RegexOptions.CultureInvariant)]
    public static partial Regex Whole();
}

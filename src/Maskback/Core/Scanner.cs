using Maskback.Detect;
using Maskback.Model;

namespace Maskback.Core;

public static class Scanner
{
    public static List<Finding> Scan(string text, IReadOnlyCollection<string> kinds)
    {
        var enabledKinds = new HashSet<string>(kinds, StringComparer.Ordinal);
        var findings = new List<Finding>();

        foreach (var (start, word) in Words.Split(text))
        {
            foreach (var kind in Kinds.All)
            {
                if (enabledKinds.Contains(kind) && Matches(kind, word))
                {
                    findings.Add(new Finding(start, kind, word));
                    break;
                }
            }
        }

        return findings;
    }

    private static bool Matches(string kind, string word)
    {
        return kind switch
        {
            "email" => Email.Matches(word),
            "key" => Key.Matches(word),
            "iban" => Iban.Matches(word),
            "card" => Card.Matches(word),
            "ipv4" => Ipv4.Matches(word),
            _ => false
        };
    }
}

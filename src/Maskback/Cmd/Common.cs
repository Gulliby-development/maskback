using System.Text;
using Maskback.Base;
using Maskback.Model;

namespace Maskback.Cmd;

public static class Common
{
    public static string[] ParseKinds(Parsed parsed)
    {
        if (!parsed.Options.TryGetValue("--kinds", out var value))
        {
            return Kinds.All;
        }

        var kinds = value.Split(',', StringSplitOptions.None);
        foreach (var kind in kinds)
        {
            if (!Kinds.All.Contains(kind, StringComparer.Ordinal))
            {
                throw new FailureException(2, $"unknown kind {kind}");
            }
        }

        return kinds;
    }

    public static string Warnings(IEnumerable<string> unknown)
    {
        var warnings = new StringBuilder();
        foreach (var placeholder in unknown)
        {
            warnings.Append("maskback: unknown placeholder ");
            warnings.Append(placeholder);
            warnings.Append('\n');
        }

        return warnings.ToString();
    }
}

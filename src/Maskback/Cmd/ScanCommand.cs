using System.Globalization;
using System.Text;
using Maskback.Base;
using Maskback.Core;
using Maskback.Model;

namespace Maskback.Cmd;

public static class ScanCommand
{
    private static readonly string[] Flags = ["--kinds"];
    private static readonly string[] Switches = ["--summary"];

    public static int Run(IReadOnlyList<string> args, Inputs inputs, Output output)
    {
        var parsed = Options.Parse(args, Flags, Switches);
        parsed.Expect(1, "scan FILE [--kinds LIST] [--summary]");
        var kinds = Common.ParseKinds(parsed);
        var (_, text) = inputs.Read(parsed.Positionals[0]);
        var findings = Scanner.Scan(text, kinds);

        if (parsed.Options.ContainsKey("--summary"))
        {
            output.WriteOut(Summary(findings, kinds));
            return findings.Count > 0 ? 1 : 0;
        }

        var rendered = new StringBuilder();
        var line = 1;
        var column = 1;
        var cursor = 0;

        foreach (var finding in findings)
        {
            while (cursor < finding.Start)
            {
                if (text[cursor] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }

                cursor++;
            }

            rendered.Append(line.ToString(CultureInfo.InvariantCulture));
            rendered.Append(':');
            rendered.Append(column.ToString(CultureInfo.InvariantCulture));
            rendered.Append(' ');
            rendered.Append(finding.Kind);
            rendered.Append(' ');
            rendered.Append(finding.Value);
            rendered.Append('\n');
        }

        rendered.Append("maskback: ");
        rendered.Append(findings.Count.ToString(CultureInfo.InvariantCulture));
        rendered.Append(" findings\n");
        output.WriteOut(rendered.ToString());

        return findings.Count > 0 ? 1 : 0;
    }

    private static string Summary(List<Finding> findings, string[] kinds)
    {
        var rendered = new StringBuilder();

        foreach (var kind in Kinds.All.Where(kind => kinds.Contains(kind, StringComparer.Ordinal)))
        {
            rendered.Append(kind);
            rendered.Append(' ');
            rendered.Append(findings.Count(finding => string.Equals(finding.Kind, kind, StringComparison.Ordinal)).ToString(CultureInfo.InvariantCulture));
            rendered.Append('\n');
        }

        rendered.Append("maskback: ");
        rendered.Append(findings.Count.ToString(CultureInfo.InvariantCulture));
        rendered.Append(" findings\n");
        return rendered.ToString();
    }
}

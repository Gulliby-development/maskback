using Maskback.Base;
using Maskback.Core;

namespace Maskback.Cmd;

public static class UnmaskCommand
{
    private static readonly string[] Flags = ["--map"];
    private static readonly string[] Switches = ["--strict"];

    public static int Run(IReadOnlyList<string> args, Inputs inputs, Output output)
    {
        var parsed = Options.Parse(args, Flags, Switches);
        parsed.Expect(1, "unmask FILE --map FILE [--strict]");

        if (!parsed.Options.TryGetValue("--map", out var mapPath))
        {
            throw new FailureException(2, "option --map is required");
        }

        var (mapLabel, mapText) = inputs.Read(mapPath);
        var table = MapFile.Read(mapText, mapLabel);
        var (_, reply) = inputs.Read(parsed.Positionals[0]);
        var (text, unknown) = Unmasker.Unmask(reply, table);

        output.WriteOut(text);
        output.WriteErr(Common.Warnings(unknown));

        return parsed.Options.ContainsKey("--strict") && unknown.Count > 0 ? 1 : 0;
    }
}

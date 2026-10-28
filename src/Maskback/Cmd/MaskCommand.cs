using Maskback.Base;
using Maskback.Core;
using Maskback.Model;

namespace Maskback.Cmd;

public static class MaskCommand
{
    private const string Usage = "mask FILE [--kinds LIST] [--map FILE]";
    private static readonly string[] Flags = ["--kinds", "--map"];
    private static readonly string[] Switches = [];

    public static int Run(IReadOnlyList<string> args, Inputs inputs, Output output)
    {
        var parsed = Options.Parse(args, Flags, Switches);
        parsed.Expect(1, Usage);
        var kinds = Common.ParseKinds(parsed);
        var input = inputs.Read(parsed.Positionals[0]);
        var masker = new Masker();
        var masked = masker.Mask(input.Text, kinds);

        if (parsed.Options.TryGetValue("--map", out var mapPath))
        {
            var map = MapFile.Write(masker.Table);
            output.WriteOut(masked);
            output.WriteFile(mapPath, map);
        }
        else
        {
            output.WriteOut(masked);
        }

        return 0;
    }
}

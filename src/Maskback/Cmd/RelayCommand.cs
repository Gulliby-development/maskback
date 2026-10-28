using Maskback.Base;
using Maskback.Fake;

namespace Maskback.Cmd;

public static class RelayCommand
{
    private static readonly string[] Flags = ["--script", "--kinds", "--sent"];
    private static readonly string[] Switches = [];

    public static int Run(IReadOnlyList<string> args, Inputs inputs, Output output)
    {
        var parsed = Options.Parse(args, Flags, Switches);
        if (parsed.Positionals.Count < 1)
        {
            throw new FailureException(2, "usage: maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]");
        }

        if (!parsed.Options.TryGetValue("--script", out var scriptPath))
        {
            throw new FailureException(2, "option --script is required");
        }

        var kinds = Common.ParseKinds(parsed);
        var (scriptLabel, scriptText) = inputs.Read(scriptPath);
        var replies = Scripted.ParseScript(scriptText, scriptLabel);
        var prompts = new List<string>();
        foreach (var path in parsed.Positionals)
        {
            var (_, prompt) = inputs.Read(path);
            prompts.Add(prompt);
        }

        var result = Relay.Run(prompts, kinds, new Scripted(replies));
        var warnings = Common.Warnings(result.Unknown);
        var sentPath = parsed.Options.GetValueOrDefault("--sent");

        output.WriteOut(result.Text);
        output.WriteErr(warnings);
        if (sentPath is not null)
        {
            output.WriteFile(sentPath, result.Log);
        }

        return 0;
    }
}

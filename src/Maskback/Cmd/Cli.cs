using Maskback.Base;

namespace Maskback.Cmd;

public static class Cli
{
    public const string Version = "0.1.0";
    public const string Usage =
        "usage: maskback mask FILE [--kinds LIST] [--map FILE]\n" +
        "       maskback scan FILE [--kinds LIST]\n" +
        "       maskback unmask FILE --map FILE [--strict]\n" +
        "       maskback relay FILE... --script FILE [--kinds LIST] [--sent FILE]\n" +
        "kinds: email, key, iban, card, ipv4\n";

    public static int Run(IReadOnlyList<string> args, Inputs inputs, Output output)
    {
        if (args.Count == 0)
        {
            output.WriteErr(Usage);
            return 2;
        }

        if (string.Equals(args[0], "help", StringComparison.Ordinal) ||
            string.Equals(args[0], "--help", StringComparison.Ordinal))
        {
            output.WriteOut(Usage);
            return 0;
        }

        if (string.Equals(args[0], "--version", StringComparison.Ordinal))
        {
            output.WriteOut($"maskback {Version}\n");
            return 0;
        }

        try
        {
            var command = args[0];
            var commandArgs = args.Skip(1).ToArray();

            if (string.Equals(command, "mask", StringComparison.Ordinal))
            {
                return MaskCommand.Run(commandArgs, inputs, output);
            }

            if (string.Equals(command, "scan", StringComparison.Ordinal))
            {
                return ScanCommand.Run(commandArgs, inputs, output);
            }

            if (string.Equals(command, "unmask", StringComparison.Ordinal))
            {
                return UnmaskCommand.Run(commandArgs, inputs, output);
            }

            if (string.Equals(command, "relay", StringComparison.Ordinal))
            {
                return RelayCommand.Run(commandArgs, inputs, output);
            }

            throw new FailureException(2, $"unknown command {command}");
        }
        catch (FailureException failure)
        {
            output.WriteErr($"maskback: {failure.Message}\n");
            return failure.Code;
        }
    }
}

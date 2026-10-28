namespace Maskback.Base;

public sealed class Inputs(Func<string, byte[]?> readFile, Func<byte[]> readStdin)
{
    private readonly Func<string, byte[]?> readFile = readFile;
    private readonly Func<byte[]> readStdin = readStdin;

    public (string Label, string Text) Read(string name)
    {
        if (string.Equals(name, "-", StringComparison.Ordinal))
        {
            const string label = "<stdin>";
            return (label, Text.Decode(readStdin(), label));
        }

        var bytes = readFile(name) ?? throw new FailureException(2, $"cannot read {name}");
        return (name, Text.Decode(bytes, name));
    }
}

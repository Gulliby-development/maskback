namespace Maskback.Base;

public sealed class Parsed
{
    public List<string> Positionals { get; } = [];
    public Dictionary<string, string> Options { get; } = new(StringComparer.Ordinal);

    public void Expect(int count, string usage)
    {
        if (Positionals.Count != count)
        {
            throw new FailureException(2, $"usage: maskback {usage}");
        }
    }
}

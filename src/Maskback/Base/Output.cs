namespace Maskback.Base;

public sealed class Output
{
    public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);
    public string Out { get; private set; } = string.Empty;
    public string Err { get; private set; } = string.Empty;

    public void WriteOut(string text)
    {
        Out += text;
    }

    public void WriteErr(string text)
    {
        Err += text;
    }

    public void WriteFile(string path, string text)
    {
        Files[path] = text;
    }
}

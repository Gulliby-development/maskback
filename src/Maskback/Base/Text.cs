using System.Text;

namespace Maskback.Base;

public static class Text
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static string Decode(byte[] bytes, string label)
    {
        try
        {
            var text = StrictUtf8.GetString(bytes);
            return text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;
        }
        catch (DecoderFallbackException)
        {
            throw new FailureException(2, $"{label}: input is not valid UTF-8");
        }
    }

    public static List<string> SplitLines(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        var lines = new List<string>();
        var start = 0;

        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n')
            {
                continue;
            }

            var end = index > start && text[index - 1] == '\r' ? index - 1 : index;
            lines.Add(text[start..end]);
            start = index + 1;
        }

        if (start < text.Length)
        {
            lines.Add(text[start..]);
        }

        return lines;
    }

    public static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            switch (character)
            {
                case '\\':
                    escaped.Append("\\\\");
                    break;
                case '\n':
                    escaped.Append("\\n");
                    break;
                case '\r':
                    escaped.Append("\\r");
                    break;
                default:
                    escaped.Append(character);
                    break;
            }
        }

        return escaped.ToString();
    }
}

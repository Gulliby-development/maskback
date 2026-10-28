namespace Maskback.Model;

public static class Words
{
    public static List<(int Start, string Word)> Split(string text)
    {
        var words = new List<(int Start, string Word)>();
        var index = 0;

        while (index < text.Length)
        {
            while (index < text.Length && IsSeparator(text[index]))
            {
                index++;
            }

            if (index == text.Length)
            {
                break;
            }

            var start = index;
            while (index < text.Length && !IsSeparator(text[index]))
            {
                index++;
            }

            var end = index;
            while (start < end && IsLeadingTrimCharacter(text[start]))
            {
                start++;
            }

            while (end > start && IsTrailingTrimCharacter(text[end - 1]))
            {
                end--;
            }

            if (start < end)
            {
                words.Add((start, text[start..end]));
            }
        }

        return words;
    }

    private static bool IsSeparator(char character)
    {
        return character is ' ' or '\t' or '\r' or '\n';
    }

    private static bool IsLeadingTrimCharacter(char character)
    {
        return character is '(' or '[' or '{' or '<' or '"' or '\'';
    }

    private static bool IsTrailingTrimCharacter(char character)
    {
        return character is ')' or ']' or '}' or '>' or '"' or '\'' or ',' or ';' or ':' or '!' or '?' or '.';
    }
}

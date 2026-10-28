namespace Maskback.Detect;

public static class Key
{
    public static bool Matches(string word)
    {
        if (word.StartsWith("sk-", StringComparison.Ordinal))
        {
            if (word.Length < 23)
            {
                return false;
            }

            for (var index = 3; index < word.Length; index++)
            {
                var character = word[index];
                if (!Chars.IsAlnum(character) && character is not '_' and not '-')
                {
                    return false;
                }
            }

            return true;
        }

        if (word.StartsWith("ghp_", StringComparison.Ordinal))
        {
            if (word.Length != 40)
            {
                return false;
            }

            for (var index = 4; index < word.Length; index++)
            {
                if (!Chars.IsAlnum(word[index]))
                {
                    return false;
                }
            }

            return true;
        }

        if (word.StartsWith("AKIA", StringComparison.Ordinal))
        {
            if (word.Length != 20)
            {
                return false;
            }

            for (var index = 4; index < word.Length; index++)
            {
                var character = word[index];
                if (!Chars.IsUpper(character) && !Chars.IsDigit(character))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }
}

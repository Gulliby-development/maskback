namespace Maskback.Detect;

public static class Iban
{
    public static bool Matches(string word)
    {
        if (word.Length is < 15 or > 34)
        {
            return false;
        }

        for (var index = 0; index < word.Length; index++)
        {
            var character = word[index];
            if (index < 2)
            {
                if (!Chars.IsUpper(character))
                {
                    return false;
                }
            }
            else if (index < 4)
            {
                if (!Chars.IsDigit(character))
                {
                    return false;
                }
            }
            else if (!Chars.IsUpper(character) && !Chars.IsDigit(character))
            {
                return false;
            }
        }

        var remainder = 0;
        for (var offset = 0; offset < word.Length; offset++)
        {
            var index = offset + 4;
            if (index >= word.Length)
            {
                index -= word.Length;
            }

            var character = word[index];
            if (Chars.IsDigit(character))
            {
                remainder = ((remainder * 10) + (character - '0')) % 97;
            }
            else
            {
                var value = (character - 'A') + 10;
                remainder = ((remainder * 10) + (value / 10)) % 97;
                remainder = ((remainder * 10) + (value % 10)) % 97;
            }
        }

        return remainder == 1;
    }
}

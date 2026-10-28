namespace Maskback.Detect;

public static class Card
{
    public static bool Matches(string word)
    {
        var digitCount = 0;
        var previousWasHyphen = false;

        for (var index = 0; index < word.Length; index++)
        {
            var character = word[index];
            if (Chars.IsDigit(character))
            {
                digitCount++;
                previousWasHyphen = false;
            }
            else if (character == '-' && digitCount > 0 && !previousWasHyphen)
            {
                previousWasHyphen = true;
            }
            else
            {
                return false;
            }
        }

        if (digitCount is < 13 or > 19 || previousWasHyphen)
        {
            return false;
        }

        var sum = 0;
        var doubleDigit = false;
        for (var index = word.Length - 1; index >= 0; index--)
        {
            var character = word[index];
            if (character == '-')
            {
                continue;
            }

            var digit = character - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }
}

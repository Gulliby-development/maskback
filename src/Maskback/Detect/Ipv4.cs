namespace Maskback.Detect;

public static class Ipv4
{
    public static bool Matches(string word)
    {
        var partCount = 0;
        var digitCount = 0;
        var value = 0;
        var startsWithZero = false;

        for (var index = 0; index <= word.Length; index++)
        {
            if (index == word.Length || word[index] == '.')
            {
                if (digitCount == 0 || digitCount > 3 || (startsWithZero && digitCount > 1) || value > 255)
                {
                    return false;
                }

                partCount++;
                if (partCount > 4)
                {
                    return false;
                }

                digitCount = 0;
                value = 0;
                startsWithZero = false;
                continue;
            }

            var character = word[index];
            if (!Chars.IsDigit(character))
            {
                return false;
            }

            if (digitCount == 0 && character == '0')
            {
                startsWithZero = true;
            }

            digitCount++;
            if (digitCount > 3)
            {
                return false;
            }

            value = (value * 10) + (character - '0');
        }

        return partCount == 4;
    }
}

namespace Maskback.Detect;

public static class Email
{
    public static bool Matches(string word)
    {
        var atIndex = -1;
        for (var index = 0; index < word.Length; index++)
        {
            if (word[index] == '@')
            {
                if (atIndex >= 0)
                {
                    return false;
                }

                atIndex = index;
            }
        }

        if (atIndex <= 0 || atIndex == word.Length - 1)
        {
            return false;
        }

        var localLength = atIndex;
        if (localLength > 64 || word[0] == '.' || word[localLength - 1] == '.')
        {
            return false;
        }

        for (var index = 0; index < localLength; index++)
        {
            var character = word[index];
            if (!Chars.IsAlnum(character) && character is not ('.' or '_' or '%' or '+' or '-'))
            {
                return false;
            }

            if (character == '.' && index > 0 && word[index - 1] == '.')
            {
                return false;
            }
        }

        var domainStart = atIndex + 1;
        var labelStart = domainStart;
        var labelCount = 0;

        for (var index = domainStart; index <= word.Length; index++)
        {
            if (index < word.Length && word[index] != '.')
            {
                var character = word[index];
                if (!Chars.IsAlnum(character) && character != '-')
                {
                    return false;
                }

                continue;
            }

            var labelLength = index - labelStart;
            if (labelLength == 0 || labelLength > 63 || word[labelStart] == '-' || word[index - 1] == '-')
            {
                return false;
            }

            labelCount++;
            if (index == word.Length)
            {
                if (labelLength < 2)
                {
                    return false;
                }

                for (var labelIndex = labelStart; labelIndex < index; labelIndex++)
                {
                    if (!Chars.IsLetter(word[labelIndex]))
                    {
                        return false;
                    }
                }
            }

            labelStart = index + 1;
        }

        return labelCount >= 2;
    }
}

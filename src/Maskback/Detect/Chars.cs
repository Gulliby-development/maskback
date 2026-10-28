namespace Maskback.Detect;

public static class Chars
{
    public static bool IsDigit(char character)
    {
        return character is >= '0' and <= '9';
    }

    public static bool IsUpper(char character)
    {
        return character is >= 'A' and <= 'Z';
    }

    public static bool IsLetter(char character)
    {
        return character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');
    }

    public static bool IsAlnum(char character)
    {
        return IsLetter(character) || IsDigit(character);
    }
}

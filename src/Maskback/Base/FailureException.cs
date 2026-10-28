namespace Maskback.Base;

public sealed class FailureException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}

using Maskback.Base;
using Maskback.Model;

namespace Maskback.Tests;

public sealed class UnitTest1
{
    [Fact]
    public void RetainsFailureCode()
    {
        var exception = new FailureException(1, "failure");
        Assert.Equal(1, exception.Code);
    }
}

public sealed class FailureExceptionTests
{
    [Fact]
    public void RetainsExceptionAndFindingValues()
    {
        var exception = new FailureException(2, "bad input");
        Assert.True(exception.Code == 2);
        Assert.True(exception.Message == "bad input");

        var finding = new Finding(3, "email", "a@b.co");
        Assert.Equal(3, finding.Start);
        Assert.Equal("email", finding.Kind);
        Assert.Equal("a@b.co", finding.Value);
        Assert.Equal(new Finding(3, "email", "a@b.co"), finding);
    }
}

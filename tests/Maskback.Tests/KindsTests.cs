using Maskback.Base;
using Maskback.Model;

namespace Maskback.Tests;

public sealed class KindsTests
{
    [Fact]
    public void ExposesCanonicalKindOrder()
    {
        var output = new Output();
        Assert.Equal(string.Empty, output.Out);
        Assert.Equal(string.Empty, output.Err);
        Assert.Empty(output.Files);

        Assert.Equal(["email", "key", "iban", "card", "ipv4"], Kinds.All);
    }
}

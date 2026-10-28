using Maskback.Base;

namespace Maskback.Tests;

public sealed class OutputTests
{
    [Fact]
    public void CollectsStreamsAndFinalFiles()
    {
        var output = new Output();

        Assert.Equal(string.Empty, output.Out);
        Assert.Equal(string.Empty, output.Err);
        Assert.Empty(output.Files);

        output.WriteOut("a");
        output.WriteOut("\n");
        output.WriteErr("bad");
        output.WriteErr(" input");
        output.WriteFile("m.txt", "old");
        output.WriteFile("m.txt", "new");

        Assert.Equal("a\n", output.Out);
        Assert.Equal("bad input", output.Err);
        var file = Assert.Single(output.Files);
        Assert.Equal("m.txt", file.Key);
        Assert.Equal("new", file.Value);
    }
}

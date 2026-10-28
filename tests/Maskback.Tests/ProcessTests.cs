using System.Diagnostics;

namespace Maskback.Tests;

public sealed class ProcessTests
{
    [Fact]
    public async Task ProgramRejectsInvalidUtf8OnStandardInput()
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "Maskback.dll");
        using var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.ArgumentList.Add(dll);
        process.StartInfo.ArgumentList.Add("scan");
        process.StartInfo.ArgumentList.Add("-");
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        await process.StandardInput.BaseStream.WriteAsync((byte[])[0x61, 0x40, 0x62, 0x2E, 0x63, 0x6F, 0x0A, 0xFF, 0x0A]);
        process.StandardInput.Close();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.Equal(string.Empty, stdout);
        Assert.Equal("maskback: <stdin>: input is not valid UTF-8\n", stderr);
        Assert.Equal(2, process.ExitCode);
    }
}

using System.Text;
using Maskback.Base;
using Maskback.Cmd;

static byte[]? ReadFile(string path)
{
    try
    {
        return File.ReadAllBytes(path);
    }
    catch (IOException)
    {
        return null;
    }
    catch (UnauthorizedAccessException)
    {
        return null;
    }
}

static byte[] ReadStdin()
{
    using var input = Console.OpenStandardInput();
    using var buffer = new MemoryStream();
    input.CopyTo(buffer);
    return buffer.ToArray();
}

var output = new Output();
var code = Cli.Run(args, new Inputs(ReadFile, ReadStdin), output);
var utf8 = new UTF8Encoding(false);
foreach (var (path, text) in output.Files)
{
    File.WriteAllBytes(path, utf8.GetBytes(text));
}

using (var stdout = Console.OpenStandardOutput())
{
    stdout.Write(utf8.GetBytes(output.Out));
}

using (var stderr = Console.OpenStandardError())
{
    stderr.Write(utf8.GetBytes(output.Err));
}

return code;

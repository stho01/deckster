using Deckster.CodeGenerator.Code;

namespace Deckster.CodeGenerator.Generators;

public abstract class ClientGenerator
{
    protected readonly SourceWriter Source = new();

    public async Task WriteToAsync(FileInfo file)
    {
        if (file.Directory is { Exists: false })
        {
            file.Directory.Create();
        }
        await using var fileStream = file.Exists ? file.Open(FileMode.Truncate) : file.Open(FileMode.CreateNew);
        await using var writer = new StreamWriter(fileStream);
        await writer.WriteAsync(Source.ToString());
        await writer.FlushAsync();
    }
}
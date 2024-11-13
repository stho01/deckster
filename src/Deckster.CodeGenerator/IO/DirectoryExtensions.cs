namespace Deckster.CodeGenerator.IO;

public static class DirectoryExtensions
{
    public static DirectoryInfo GetSubDirectory(this DirectoryInfo directory, params string[] parts)
    {
        var path = Path.Combine(new []{directory.FullName}.Concat(parts).ToArray());
        return new DirectoryInfo(path);
    }
    
    public static DirectoryInfo GetCleanSubDirectory(this DirectoryInfo directory, params string[] parts)
    {
        var path = Path.Combine(new []{directory.FullName}.Concat(parts).ToArray());
        var sub = new DirectoryInfo(path);
        
        if (sub.Exists)
        {
            sub.Delete(true);
        }
        sub.Create();
        
        return sub;
    }

    public static FileInfo GetFile(this DirectoryInfo directory, params string[] parts)
    {
        var path = Path.Combine(new []{directory.FullName}.Concat(parts).ToArray());
        return new FileInfo(path);
    }
}

public static class FileExtensions
{
    public static async Task WriteAllTextAsync(this FileInfo file, string content, CancellationToken cancellationToken = default)
    {
        if (file.Exists)
        {
            file.Delete();
        }

        await using var stream = file.Create();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        await writer.FlushAsync(cancellationToken);
    }
}
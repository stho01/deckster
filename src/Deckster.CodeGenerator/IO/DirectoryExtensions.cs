namespace Deckster.CodeGenerator.IO;

public static class DirectoryExtensions
{
    public static DirectoryInfo GetSubDirectory(this DirectoryInfo directory, params string[] parts)
    {
        var path = Path.Combine(new []{directory.FullName}.Concat(parts).ToArray());
        return new DirectoryInfo(path);
    }

    public static FileInfo GetFile(this DirectoryInfo directory, params string[] parts)
    {
        var path = Path.Combine(new []{directory.FullName}.Concat(parts).ToArray());
        return new FileInfo(path);
    }
}
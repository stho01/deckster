using System.Text;
using Deckster.Server.CodeGeneration.Meta;
using StringExtensions = Deckster.Client.Sugar.StringExtensions;

namespace Deckster.Server.CodeGeneration;

public class KotlinGenerator
{
    private readonly int _indent;
    private readonly StringBuilder _builder = new();

    public KotlinGenerator(ServiceMeta meta, string ns)
    {
        AppendLine($"package {ns}");
        AppendLine();
        AppendLine($"interface {meta.Name} {{");
        _indent++;
        foreach (var method in meta.Methods)
        {
            var builder = new StringBuilder($"suspend fun {StringExtensions.ToCamelCase(method.Name)}({string.Join(", ", method.Parameters.Select(FormatParameter))})");
            if (method.ReturnType != "void")
            {
                builder.Append($": {method.ReturnType}");
            }
            AppendLine(builder.ToString());
        }
        _indent--;
        AppendLine("}");
    }

    public async Task WriteToAsync(string path)
    {
        var file = new FileInfo(path);
        if (file.Directory is { Exists: false })
        {
            file.Directory.Create();
        }
        await using var fileStream = file.Exists ? file.Open(FileMode.Truncate) : file.Open(FileMode.CreateNew);
        await using var writer = new StreamWriter(fileStream);
        await writer.WriteAsync(_builder.ToString());
        await writer.FlushAsync();
    }

    public string Build() => _builder.ToString();

    private static string FormatParameter(ParameterMeta parameter)
    {
        return $"{parameter.Name}: {parameter.Type}";
    }

    private void AppendLine() => _builder.AppendLine();
    
    private void AppendLine(string line)
    {
        for (var ii = 0; ii < _indent; ii++)
        {
            _builder.Append("  ");
        }

        _builder.AppendLine(line);
    }
}
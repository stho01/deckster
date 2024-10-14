using System.Text;
using Deckster.Client.Sugar;

namespace Deckster.Server.Games.Common.Meta;

public class KotlinBuilder
{
    private readonly int _indent;
    private readonly StringBuilder _builder = new();

    public KotlinBuilder(ServiceMeta meta)
    {
        AppendLine($"interface {meta.Name} {{");
        _indent++;
        foreach (var method in meta.Methods)
        {
            var builder = new StringBuilder($"suspend fun {method.Name.ToCamelCase()}({string.Join(", ", method.Parameters.Select(FormatParameter))})");
            if (method.ReturnType != "void")
            {
                builder.Append($": {method.ReturnType}");
            }
            AppendLine(builder.ToString());
        }
        _indent--;
        AppendLine("}");
    }

    public string Build() => _builder.ToString();

    private static string FormatParameter(ParameterMeta parameter)
    {
        return $"{parameter.Name}: {parameter.Type}";
    }

    private void AppendLine(string line)
    {
        for (var ii = 0; ii < _indent; ii++)
        {
            _builder.Append("  ");
        }

        _builder.AppendLine(line);
    }
}
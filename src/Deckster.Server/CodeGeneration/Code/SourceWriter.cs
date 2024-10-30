using System.Text;

namespace Deckster.Server.CodeGeneration.Code;

public class SourceWriterOptions
{
    public int IndentationWidth { get; init; } = 4;
}

public class SourceWriter
{
    private bool _appending;
    private int _indentation;
    private readonly string _indent;
    
    private readonly StringBuilder _builder = new();

    public SourceWriter() : this(new())
    {
        
    }

    public SourceWriter(SourceWriterOptions options)
    {
        _indent = string.Join("", Enumerable.Range(0, options.IndentationWidth).Select(_ => ' '));
    }

    public Indenter Indent()
    {
        IncreaseIndent();
        return new Indenter(DecreaseIndent);
    }
    
    public Indenter StartBlock()
    {
        AppendLine("{");
        IncreaseIndent();
        return new Indenter(EndBlock);
    }

    private void EndBlock()
    {
        DecreaseIndent();
        AppendLine("}");
    }

    private void IncreaseIndent()
    {
        _indentation++;
    }

    private void DecreaseIndent()
    {
        if (_indentation > 0)
        {
            _indentation--;
        }
    }

    public SourceWriter AppendLine()
    {
        _builder.AppendLine();
        _appending = false;
        return this;
    }

    public SourceWriter Append(string text)
    {
        if (!_appending)
        {
            AppendIndentation();
            _appending = true;
        }

        _builder.Append(text);
        return this;
    }
    
    public SourceWriter AppendLine(string line)
    {
        var lines = line.Split('\r', '\n');
        return AppendLines(lines);
    }

    public SourceWriter AppendLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            AppendIndentation();
            _builder.AppendLine(line);
            _appending = false;
        }
        _appending = false;

        return this;
    }

    private SourceWriter AppendIndentation()
    {
        if (_appending)
        {
            return this;
        }
        for (var ii = 0; ii < _indentation; ii++)
        {
            _builder.Append(_indent);
        }

        return this;
    }

    public override string ToString() => _builder.ToString();
}
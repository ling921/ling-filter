using System.Diagnostics;
using System.Text;

namespace Ling.Filter.Generators.Helpers;

/// <summary>
/// Indentation-aware C# source text builder for code generation.
/// </summary>
[DebuggerDisplay("Length: {Length}, {ToString()}")]
internal sealed class CodeBuilder
{
    private readonly StringBuilder _sb;
    private int _indentLevel;
    private bool _newLine;

    /// <summary>
    /// Gets the number of spaces per indentation level.
    /// </summary>
    public int IndentSize { get; }

    /// <summary>
    /// Gets the current character length of the underlying buffer.
    /// </summary>
    public int Length => _sb.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBuilder"/> class.
    /// </summary>
    public CodeBuilder(int indentSize = 4)
    {
        if (indentSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indentSize));
        }

        _indentLevel = 0;
        _newLine = true;
        _sb = new StringBuilder();
        IndentSize = indentSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBuilder"/> class seeded with <paramref name="value"/>.
    /// </summary>
    public CodeBuilder(string value, int indentSize = 4)
    {
        if (indentSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indentSize));
        }

        _indentLevel = 0;
        _newLine = value.EndsWith("\n", StringComparison.Ordinal);
        _sb = new StringBuilder(value);
        IndentSize = indentSize;
    }

    public CodeBuilder OpenBrace()
    {
        AppendLine("{");
        _indentLevel++;
        _newLine = true;
        return this;
    }

    public CodeBuilder CloseBrace(string? textAfterBrace = null, bool appendSemicolon = false)
    {
        if (_indentLevel <= 0)
        {
            throw new InvalidOperationException("Cannot close brace at indent level 0.");
        }

        _indentLevel--;
        Append("}");
        if (textAfterBrace is not null)
        {
            Append(textAfterBrace);
        }

        if (appendSemicolon)
        {
            Append(";");
        }

        AppendLine();
        _newLine = true;
        return this;
    }

    public CodeBuilder CloseAllBraces()
    {
        while (_indentLevel > 0)
        {
            CloseBrace();
        }

        return this;
    }

    public CodeBuilder Append(string text)
    {
        EnsureIndent();
        _sb.Append(text);
        return this;
    }

    public CodeBuilder AppendLine(string text)
    {
        EnsureIndent();
        _sb.AppendLine(text);
        _newLine = true;
        return this;
    }

    public CodeBuilder AppendLine()
    {
        _sb.AppendLine();
        _newLine = true;
        return this;
    }

    public CodeBuilder AppendFormatLine(string format, params object?[] args)
    {
        EnsureIndent();
        _sb.AppendFormat(format, args);
        AppendLine();
        return this;
    }

    public override string ToString()
    {
        CloseAllBraces();
        return _sb.ToString();
    }

    private void EnsureIndent()
    {
        if (_newLine && _indentLevel > 0)
        {
            _sb.Append(' ', IndentSize * _indentLevel);
        }

        _newLine = false;
    }
}

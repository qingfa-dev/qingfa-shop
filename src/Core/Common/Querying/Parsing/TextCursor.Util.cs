using System.Text;

namespace Core.Common.Querying.Parsing;

/// <summary>Cursor over one filter string; honors backslash escaping.</summary>
internal sealed class TextCursor(string text)
{
    private int _position;

    public bool AtEnd => _position >= text.Length;
    public char Peek() => AtEnd ? '\0' : text[_position];

    public char Read() => AtEnd ? '\0' : text[_position++];

    public void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(Peek()))
            _position++;
    }

    public string ReadField()
    {
        SkipWhitespace();
        int start = _position;
        while (!AtEnd && !QueryConstant.Tokens.FieldTerminators.Contains(Peek()))
        {
            _position++;
        }

        return text[start.._position].Trim();
    }

    public string ReadOperator()
    {
        SkipWhitespace();
        if (AtEnd)
            return string.Empty;

        if (Peek() == QueryConstant.Tokens.Operator.Custom[0])
        {
            int start = _position++;
            while (!AtEnd && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
                _position++;
            return text[start.._position];
        }

        foreach (string candidate in QueryConstant.Tokens.TwoCharacterOperators)
        {
            if (_position + 1 < text.Length &&
                text[_position] == candidate[0] &&
                text[_position + 1] == candidate[1])
            {
                _position += 2;
                return candidate;
            }
        }

        char one = Read();
        return QueryConstant.Tokens.OneCharacterOperators.Contains(one.ToString())
            ? one.ToString()
            : string.Empty;
    }

    public string ReadValue(out bool caseInsensitive)
    {
        caseInsensitive = false;
        SkipWhitespace();
        var value = new StringBuilder();

        while (!AtEnd)
        {
            char current = Peek();

            if (current == QueryConstant.Tokens.Character.Escape && _position + 1 < text.Length)
            {
                value.Append(text[_position + 1]);
                _position += 2;
                continue;
            }

            if (IsValueTerminator(current))
                break;

            if (current == QueryConstant.Tokens.Character.CaseInsensitive && _position + 1 < text.Length &&
                text[_position + 1] == QueryConstant.Tokens.Character.CaseInsensitiveMarker &&
                IsValueBoundary(_position + 2))
            {
                caseInsensitive = true;
                _position += 2;
                break;
            }

            value.Append(current);
            _position++;
        }

        return value.ToString().Trim();
    }

    private static bool IsValueTerminator(char current) =>
        current is QueryConstant.Tokens.Character.And
            or QueryConstant.Tokens.Character.Or
            or QueryConstant.Tokens.Character.GroupClose;

    private bool IsValueBoundary(int index) =>
        index >= text.Length ||
        IsValueTerminator(text[index]) ||
        char.IsWhiteSpace(text[index]);
}
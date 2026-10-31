using System.Text;

namespace Application.BuildingBlocks.Querying.Parsing;

/// <summary>Cursor over one filter string; honors backslash escaping.</summary>
internal sealed class TextCursor(string text)
{
    private int _position;

    /// <summary>True when the cursor has reached or passed the end of the text.</summary>
    public bool AtEnd => _position >= text.Length;

    /// <summary>Peeks at the current character without advancing the cursor.</summary>
    /// <returns>The current character, or '\0' if at the end.</returns>
    public char Peek() => AtEnd ? '\0' : text[_position];

    /// <summary>Reads the current character and advances the cursor.</summary>
    /// <returns>The current character, or '\0' if at the end.</returns>
    public char Read() => AtEnd ? '\0' : text[_position++];

    /// <summary>Skips whitespace characters (spaces, tabs).</summary>
    public void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(Peek()))
            _position++;
    }

    /// <summary>Reads a field name until a terminator character (operator, separator, etc.).</summary>
    /// <returns>The trimmed field name.</returns>
    public string ReadField()
    {
        // Parse: field names end at operators, separators, or parentheses — the terminator
        // set defines the boundary between a field identifier and the next grammar token.
        SkipWhitespace();
        int start = _position;
        while (!AtEnd && !QueryConstant.Tokens.FieldTerminators.Contains(Peek()))
        {
            _position++;
        }

        return text[start.._position].Trim();
    }

    /// <summary>
    /// Reads an operator token, trying two-character operators first, then one-character operators,
    /// and finally custom operators prefixed with '#'.
    /// </summary>
    /// <returns>The operator string, or empty if no operator is found.</returns>
    public string ReadOperator()
    {
        SkipWhitespace();
        if (AtEnd)
            return string.Empty;

        // Parse: custom operators start with '#' and are read as a single alphanumeric token.
        if (Peek() == QueryConstant.Tokens.Operator.Custom[0])
        {
            int start = _position++;
            while (!AtEnd && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
                _position++;
            return text[start.._position];
        }

        // Priority: check two-character operators first so `!=` is not misread as `!` followed by `=`.
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

        // Fallback: single-character operators are checked last.
        char one = Read();
        return QueryConstant.Tokens.OneCharacterOperators.Contains(one.ToString())
            ? one.ToString()
            : string.Empty;
    }

    /// <summary>
    /// Reads a value token, handling escape sequences and the optional case-insensitive marker (/i).
    /// </summary>
    /// <param name="caseInsensitive">True if the value ended with the /i marker.</param>
    /// <returns>The trimmed value string.</returns>
    public string ReadValue(out bool caseInsensitive)
    {
        caseInsensitive = false;
        SkipWhitespace();
        var value = new StringBuilder();

        while (!AtEnd)
        {
            char current = Peek();

            // Escape: a backslash consumes the next character literally, so `\,` includes
            // a comma in the value instead of terminating it.
            if (current == QueryConstant.Tokens.Character.Escape && _position + 1 < text.Length)
            {
                value.Append(text[_position + 1]);
                _position += 2;
                continue;
            }

            // Terminate: comma, pipe, and closing parenthesis end a value token.
            if (IsValueTerminator(current))
                break;

            // Parse: the /i marker at the end of a value signals case-insensitive comparison.
            // It must be followed by a boundary (terminator, whitespace, or end of text)
            // so that "/i" inside a value is not mistaken for the marker.
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

    /// <summary>Checks whether a character terminates a value token.</summary>
    /// <param name="current">The character to check.</param>
    /// <returns>True if the character is a comma, pipe, or closing parenthesis.</returns>
    private static bool IsValueTerminator(char current) =>
        // Grammar: these three characters mark the boundary between a value and the next clause.
        current is QueryConstant.Tokens.Character.And
            or QueryConstant.Tokens.Character.Or
            or QueryConstant.Tokens.Character.GroupClose;

    /// <summary>Checks whether the position after a potential marker is a valid boundary.</summary>
    /// <param name="index">The position to check.</param>
    /// <returns>True if the position is at the end, a terminator, or whitespace.</returns>
    private bool IsValueBoundary(int index) =>
        // Validate: the /i marker must be at a boundary so "hello/iworld" does not trigger it.
        index >= text.Length ||
        IsValueTerminator(text[index]) ||
        char.IsWhiteSpace(text[index]);
}
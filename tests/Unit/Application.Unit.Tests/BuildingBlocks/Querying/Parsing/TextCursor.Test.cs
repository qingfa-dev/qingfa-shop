using Application.BuildingBlocks.Querying.Parsing;

namespace Application.Unit.Tests.BuildingBlocks.Querying.Parsing;


public sealed class TextCursorTest
{
    [Fact(DisplayName = "Constructor With Text Initializes At Start")]
    public void Constructor_WithText_InitializesAtStart()
    {
        var cursor = new TextCursor("hello");

        cursor.AtEnd.Should().BeFalse();
        cursor.Peek().Should().Be('h');
    }

    [Fact(DisplayName = "Constructor With Empty Text Is At End")]
    public void Constructor_WithEmptyText_IsAtEnd()
    {
        var cursor = new TextCursor("");

        cursor.AtEnd.Should().BeTrue();
        cursor.Peek().Should().Be('\0');
    }

    [Fact(DisplayName = "Read Returns Current Char And Advances")]
    public void Read_ReturnsCurrentCharAndAdvances()
    {
        var cursor = new TextCursor("hello");

        cursor.Read().Should().Be('h');
        cursor.Peek().Should().Be('e');
        cursor.Read().Should().Be('e');
    }

    [Fact(DisplayName = "Read At End Returns Null Char")]
    public void Read_AtEnd_ReturnsNullChar()
    {
        var cursor = new TextCursor("a");

        cursor.Read(); // 'a'
        cursor.Read().Should().Be('\0');
        cursor.AtEnd.Should().BeTrue();
    }

    [Fact(DisplayName = "Skip Whitespace Advances Past Spaces")]
    public void SkipWhitespace_AdvancesPastSpaces()
    {
        var cursor = new TextCursor("  hello");

        cursor.SkipWhitespace();
        cursor.Peek().Should().Be('h');
    }

    [Fact(DisplayName = "Skip Whitespace Advances Past Tabs")]
    public void SkipWhitespace_AdvancesPastTabs()
    {
        var cursor = new TextCursor("\t\thello");

        cursor.SkipWhitespace();
        cursor.Peek().Should().Be('h');
    }

    [Fact(DisplayName = "Skip Whitespace Advances Past Newlines")]
    public void SkipWhitespace_AdvancesPastNewlines()
    {
        var cursor = new TextCursor("\n\r hello");

        cursor.SkipWhitespace();
        cursor.Peek().Should().Be('h');
    }

    [Fact(DisplayName = "Skip Whitespace At End Does Nothing")]
    public void SkipWhitespace_AtEnd_DoesNothing()
    {
        var cursor = new TextCursor("");

        cursor.SkipWhitespace();
        cursor.AtEnd.Should().BeTrue();
    }

    [Fact(DisplayName = "Read Field Reads Until Terminator")]
    public void ReadField_ReadsUntilTerminator()
    {
        var cursor = new TextCursor("field=value");

        string field = cursor.ReadField();

        field.Should().Be("field");
        cursor.Peek().Should().Be('=');
    }

    [Fact(DisplayName = "Read Field With Nested Path Reads Full Path")]
    public void ReadField_WithNestedPath_ReadsFullPath()
    {
        var cursor = new TextCursor("user.name=value");

        string field = cursor.ReadField();

        field.Should().Be("user.name");
        cursor.Peek().Should().Be('=');
    }

    [Fact(DisplayName = "Read Field Skips Leading Whitespace")]
    public void ReadField_SkipsLeadingWhitespace()
    {
        var cursor = new TextCursor("  field=value");

        string field = cursor.ReadField();

        field.Should().Be("field");
    }

    [Fact(DisplayName = "Read Field At End Returns Empty")]
    public void ReadField_AtEnd_ReturnsEmpty()
    {
        var cursor = new TextCursor("");

        string field = cursor.ReadField();

        field.Should().BeEmpty();
    }

    [Fact(DisplayName = "Read Operator Reads Two Char Operators")]
    public void ReadOperator_ReadsTwoCharOperators()
    {
        var cursor = new TextCursor("field>=value");

        cursor.ReadField(); // consume "field"
        string op = cursor.ReadOperator();

        op.Should().Be(">=");
    }

    [Fact(DisplayName = "Read Operator Reads Single Char Operators")]
    public void ReadOperator_ReadsSingleCharOperators()
    {
        var cursor = new TextCursor("field=value");

        cursor.ReadField(); // consume "field"
        string op = cursor.ReadOperator();

        op.Should().Be("=");
    }

    [Theory(DisplayName = "Read Operator All Known Operators Recognized")]
    [InlineData("=")]
    [InlineData("!=")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData(">=")]
    [InlineData("<=")]
    [InlineData("=*")]
    [InlineData("!*")]
    [InlineData("^")]
    [InlineData("!^")]
    [InlineData("$")]
    [InlineData("!$")]
    public void ReadOperator_AllKnownOperators_Recognized(string op)
    {
        var cursor = new TextCursor($"field{op}value");

        cursor.ReadField();
        string readOp = cursor.ReadOperator();

        readOp.Should().Be(op);
    }

    [Fact(DisplayName = "Read Operator Custom Operator Reads Prefix And Name")]
    public void ReadOperator_CustomOperator_ReadsPrefixAndName()
    {
        var cursor = new TextCursor("field#In value");

        cursor.ReadField();
        string op = cursor.ReadOperator();

        op.Should().Be("#In");
    }

    [Fact(DisplayName = "Read Operator Unknown Single Char Returns Empty")]
    public void ReadOperator_UnknownSingleChar_ReturnsEmpty()
    {
        var cursor = new TextCursor("field~value");

        cursor.ReadField();
        string op = cursor.ReadOperator();

        op.Should().BeEmpty();
    }

    [Fact(DisplayName = "Read Value Reads Simple Value")]
    public void ReadValue_ReadsSimpleValue()
    {
        var cursor = new TextCursor("field=hello");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out bool ci);

        value.Should().Be("hello");
        ci.Should().BeFalse();
    }

    [Fact(DisplayName = "Read Value With Escaped Comma Unescapes")]
    public void ReadValue_WithEscapedComma_Unescapes()
    {
        var cursor = new TextCursor(@"field=hello\,world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello,world");
    }

    [Fact(DisplayName = "Read Value With Escaped Operator Unescapes")]
    public void ReadValue_WithEscapedOperator_Unescapes()
    {
        var cursor = new TextCursor(@"field=hello\=world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello=world");
    }

    [Fact(DisplayName = "Read Value With Escaped Paren Unescapes")]
    public void ReadValue_WithEscapedParen_Unescapes()
    {
        var cursor = new TextCursor(@"field=hello\)world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello)world");
    }

    [Fact(DisplayName = "Read Value With Case Insensitive Marker Sets Flag")]
    public void ReadValue_WithCaseInsensitiveMarker_SetsFlag()
    {
        var cursor = new TextCursor("field=HELLO/i");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out bool ci);

        value.Should().Be("HELLO");
        ci.Should().BeTrue();
    }

    [Fact(DisplayName = "Read Value Case Insensitive At End Stops At Boundary")]
    public void ReadValue_CaseInsensitiveAtEnd_StopsAtBoundary()
    {
        var cursor = new TextCursor("field=HELLO/i,other=value");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out bool ci);

        value.Should().Be("HELLO");
        ci.Should().BeTrue();
        cursor.Peek().Should().Be(',');
    }

    [Fact(DisplayName = "Read Value Stops At And")]
    public void ReadValue_StopsAtAnd()
    {
        var cursor = new TextCursor("field=hello,other=world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
        cursor.Peek().Should().Be(',');
    }

    [Fact(DisplayName = "Read Value Stops At Or")]
    public void ReadValue_StopsAtOr()
    {
        var cursor = new TextCursor("field=hello|other=world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
        cursor.Peek().Should().Be('|');
    }

    [Fact(DisplayName = "Read Value Stops At Close Paren")]
    public void ReadValue_StopsAtCloseParen()
    {
        var cursor = new TextCursor("field=hello)");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
        cursor.Peek().Should().Be(')');
    }

    [Fact(DisplayName = "Read Value Empty Value Returns Empty")]
    public void ReadValue_EmptyValue_ReturnsEmpty()
    {
        var cursor = new TextCursor("field=");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().BeEmpty();
    }

    [Fact(DisplayName = "Read Value Trims Whitespace")]
    public void ReadValue_TrimsWhitespace()
    {
        var cursor = new TextCursor("field=  hello  ");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
    }
}
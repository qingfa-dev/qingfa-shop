using Application.Common.Querying.Parsing;

namespace Application.Tests.Common.Querying.Parsing;

public sealed class TextCursorTest
{
    [Fact]
    public void Constructor_WithText_InitializesAtStart()
    {
        var cursor = new TextCursor("hello");

        cursor.AtEnd.Should().BeFalse();
        cursor.Peek().Should().Be('h');
    }

    [Fact]
    public void Constructor_WithEmptyText_IsAtEnd()
    {
        var cursor = new TextCursor("");

        cursor.AtEnd.Should().BeTrue();
        cursor.Peek().Should().Be('\0');
    }

    [Fact]
    public void Read_ReturnsCurrentCharAndAdvances()
    {
        var cursor = new TextCursor("hello");

        cursor.Read().Should().Be('h');
        cursor.Peek().Should().Be('e');
        cursor.Read().Should().Be('e');
    }

    [Fact]
    public void Read_AtEnd_ReturnsNullChar()
    {
        var cursor = new TextCursor("a");

        cursor.Read(); // 'a'
        cursor.Read().Should().Be('\0');
        cursor.AtEnd.Should().BeTrue();
    }

    [Fact]
    public void SkipWhitespace_AdvancesPastSpaces()
    {
        var cursor = new TextCursor("  hello");

        cursor.SkipWhitespace();
        cursor.Peek().Should().Be('h');
    }

    [Fact]
    public void SkipWhitespace_AdvancesPastTabs()
    {
        var cursor = new TextCursor("\t\thello");

        cursor.SkipWhitespace();
        cursor.Peek().Should().Be('h');
    }

    [Fact]
    public void SkipWhitespace_AdvancesPastNewlines()
    {
        var cursor = new TextCursor("\n\r hello");

        cursor.SkipWhitespace();
        cursor.Peek().Should().Be('h');
    }

    [Fact]
    public void SkipWhitespace_AtEnd_DoesNothing()
    {
        var cursor = new TextCursor("");

        cursor.SkipWhitespace();
        cursor.AtEnd.Should().BeTrue();
    }

    [Fact]
    public void ReadField_ReadsUntilTerminator()
    {
        var cursor = new TextCursor("field=value");

        string field = cursor.ReadField();

        field.Should().Be("field");
        cursor.Peek().Should().Be('=');
    }

    [Fact]
    public void ReadField_WithNestedPath_ReadsFullPath()
    {
        var cursor = new TextCursor("user.name=value");

        string field = cursor.ReadField();

        field.Should().Be("user.name");
        cursor.Peek().Should().Be('=');
    }

    [Fact]
    public void ReadField_SkipsLeadingWhitespace()
    {
        var cursor = new TextCursor("  field=value");

        string field = cursor.ReadField();

        field.Should().Be("field");
    }

    [Fact]
    public void ReadField_AtEnd_ReturnsEmpty()
    {
        var cursor = new TextCursor("");

        string field = cursor.ReadField();

        field.Should().BeEmpty();
    }

    [Fact]
    public void ReadOperator_ReadsTwoCharOperators()
    {
        var cursor = new TextCursor("field>=value");

        cursor.ReadField(); // consume "field"
        string op = cursor.ReadOperator();

        op.Should().Be(">=");
    }

    [Fact]
    public void ReadOperator_ReadsSingleCharOperators()
    {
        var cursor = new TextCursor("field=value");

        cursor.ReadField(); // consume "field"
        string op = cursor.ReadOperator();

        op.Should().Be("=");
    }

    [Theory]
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

    [Fact]
    public void ReadOperator_CustomOperator_ReadsPrefixAndName()
    {
        var cursor = new TextCursor("field#In value");

        cursor.ReadField();
        string op = cursor.ReadOperator();

        op.Should().Be("#In");
    }

    [Fact]
    public void ReadOperator_UnknownSingleChar_ReturnsEmpty()
    {
        var cursor = new TextCursor("field~value");

        cursor.ReadField();
        string op = cursor.ReadOperator();

        op.Should().BeEmpty();
    }

    [Fact]
    public void ReadValue_ReadsSimpleValue()
    {
        var cursor = new TextCursor("field=hello");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out bool ci);

        value.Should().Be("hello");
        ci.Should().BeFalse();
    }

    [Fact]
    public void ReadValue_WithEscapedComma_Unescapes()
    {
        var cursor = new TextCursor(@"field=hello\,world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello,world");
    }

    [Fact]
    public void ReadValue_WithEscapedOperator_Unescapes()
    {
        var cursor = new TextCursor(@"field=hello\=world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello=world");
    }

    [Fact]
    public void ReadValue_WithEscapedParen_Unescapes()
    {
        var cursor = new TextCursor(@"field=hello\)world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello)world");
    }

    [Fact]
    public void ReadValue_WithCaseInsensitiveMarker_SetsFlag()
    {
        var cursor = new TextCursor("field=HELLO/i");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out bool ci);

        value.Should().Be("HELLO");
        ci.Should().BeTrue();
    }

    [Fact]
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

    [Fact]
    public void ReadValue_StopsAtAnd()
    {
        var cursor = new TextCursor("field=hello,other=world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
        cursor.Peek().Should().Be(',');
    }

    [Fact]
    public void ReadValue_StopsAtOr()
    {
        var cursor = new TextCursor("field=hello|other=world");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
        cursor.Peek().Should().Be('|');
    }

    [Fact]
    public void ReadValue_StopsAtCloseParen()
    {
        var cursor = new TextCursor("field=hello)");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
        cursor.Peek().Should().Be(')');
    }

    [Fact]
    public void ReadValue_EmptyValue_ReturnsEmpty()
    {
        var cursor = new TextCursor("field=");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().BeEmpty();
    }

    [Fact]
    public void ReadValue_TrimsWhitespace()
    {
        var cursor = new TextCursor("field=  hello  ");

        cursor.ReadField();
        cursor.ReadOperator();
        string value = cursor.ReadValue(out _);

        value.Should().Be("hello");
    }
}
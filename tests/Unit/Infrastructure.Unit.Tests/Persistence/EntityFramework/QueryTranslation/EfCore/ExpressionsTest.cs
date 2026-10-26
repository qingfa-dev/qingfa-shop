using AwesomeAssertions;
using System.Linq.Expressions;
using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.UnitTests.Persistence.EntityFramework.QueryTranslation.EfCore;

[Trait("Category", "Unit")]
public sealed class ExpressionsTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>();

    [Fact(DisplayName = "BuildMember_WithValidField_ReturnsExpression")]
    public void BuildMember_WithValidField_ReturnsExpression()
    {
        var field = Map.Fields.First(f => f.Name == "name");
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = Translator.BuildMember(field, parameter, indexValue: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact(DisplayName = "BuildMember_WithNullExpression_ReturnsNotFilterableError")]
    public void BuildMember_WithNullExpression_ReturnsNotFilterableError()
    {
        var field = new QueryField
        {
            Name = "test",
            Expression = null,
            ValueType = typeof(string),
            ComparisonType = typeof(string)
        };
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = Translator.BuildMember(field, parameter, indexValue: null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotFilterable");
    }

    [Fact(DisplayName = "BuildMember_WithIndexValue_ReturnsIndexedExpression")]
    public void BuildMember_WithIndexValue_ReturnsIndexedExpression()
    {
        var field = new QueryField
        {
            Name = "indexed",
            Expression = Expression.Lambda<Func<QueryProbe, string, string>>(
                (QueryProbe p, string idx) => p.Name + idx,
                Expression.Parameter(typeof(QueryProbe), "p"),
                Expression.Parameter(typeof(string), "idx")),
            ValueType = typeof(string),
            ComparisonType = typeof(string),
            IndexType = typeof(string)
        };
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = Translator.BuildMember(field, parameter, "test");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "BuildMember_WithIndexButNoIndexType_ReturnsError")]
    public void BuildMember_WithIndexButNoIndexType_ReturnsError()
    {
        var field = new QueryField
        {
            Name = "indexed",
            Expression = Expression.Lambda<Func<QueryProbe, string, string>>(
                (QueryProbe p, string idx) => p.Name + idx,
                Expression.Parameter(typeof(QueryProbe), "p"),
                Expression.Parameter(typeof(string), "idx")),
            ValueType = typeof(string),
            ComparisonType = typeof(string),
            IndexType = null
        };
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = Translator.BuildMember(field, parameter, "test");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotFilterable");
    }

    [Fact(DisplayName = "BuildSafeObject_WrapsNullGuard")]
    public void BuildSafeObject_WrapsNullGuard()
    {
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");
        var member = Expression.Property(parameter, "Name");

        var result = QueryTranslationEfCore<QueryProbe>.BuildSafeObject(member);

        result.Should().BeOfType<ConditionalExpression>();
    }

    [Fact(DisplayName = "ApplyGuard_AddsNullGuardForNullableMember")]
    public void ApplyGuard_AddsNullGuardForNullableMember()
    {
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");
        var member = Expression.Property(parameter, "Name");
        var body = Expression.Equal(member, Expression.Constant("test"));

        var result = QueryTranslationEfCore<QueryProbe>.ApplyGuard(member, true, body);

        result.Should().BeOfType<BinaryExpression>();
        result.NodeType.Should().Be(ExpressionType.AndAlso);
    }

    [Fact(DisplayName = "ApplyGuard_NoGuardForNonNullableMember")]
    public void ApplyGuard_NoGuardForNonNullableMember()
    {
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");
        var member = Expression.Property(parameter, "Score");
        var body = Expression.Equal(member, Expression.Constant(10));

        var result = QueryTranslationEfCore<QueryProbe>.ApplyGuard(member, true, body);

        result.Should().BeSameAs(body);
    }

    [Fact(DisplayName = "BuildGuard_ReturnsNullForParameterExpression")]
    public void BuildGuard_ReturnsNullForParameterExpression()
    {
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = QueryTranslationEfCore<QueryProbe>.BuildGuard(parameter, false);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "Replace_ReplacesParameterInExpression")]
    public void Replace_ReplacesParameterInExpression()
    {
        var from = Expression.Parameter(typeof(QueryProbe), "old");
        var to = Expression.Parameter(typeof(QueryProbe), "new");
        var body = Expression.Property(from, "Name");

        var result = QueryTranslationEfCore<QueryProbe>.Replace(from, to, body);

        result.Should().BeOfType<MemberExpression>();
        ((MemberExpression)result).Expression.Should().BeSameAs(to);
    }

    [Fact(DisplayName = "Replace_LeavesOtherParametersUnchanged")]
    public void Replace_LeavesOtherParametersUnchanged()
    {
        var from = Expression.Parameter(typeof(QueryProbe), "old");
        var to = Expression.Parameter(typeof(QueryProbe), "new");
        var other = Expression.Parameter(typeof(string), "other");
        var body = Expression.Call(
            typeof(string).GetMethod(nameof(string.Contains))!,
            Expression.Property(from, "Name"),
            other);

        var result = QueryTranslationEfCore<QueryProbe>.Replace(from, to, body);

        var call = result.Should().BeOfType<MethodCallExpression>().Subject;
        call.Object.Should().BeSameAs(to);
        call.Arguments[0].Should().BeSameAs(other);
    }

    [Fact(DisplayName = "Unwrap_RemovesConvertUnaryExpressions")]
    public void Unwrap_RemovesConvertUnaryExpressions()
    {
        var inner = Expression.Constant(42);
        var convert = Expression.Convert(inner, typeof(object));

        var result = QueryTranslationEfCore<QueryProbe>.Unwrap(convert);

        result.Should().BeSameAs(inner);
    }

    [Fact(DisplayName = "Unwrap_RemovesNestedConverts")]
    public void Unwrap_RemovesNestedConverts()
    {
        var inner = Expression.Constant(42);
        var convert1 = Expression.Convert(inner, typeof(object));
        var convert2 = Expression.Convert(convert1, typeof(int?));

        var result = QueryTranslationEfCore<QueryProbe>.Unwrap(convert2);

        result.Should().BeSameAs(inner);
    }

    [Fact(DisplayName = "Unwrap_ReturnsSameForNonConvertExpression")]
    public void Unwrap_ReturnsSameForNonConvertExpression()
    {
        var constant = Expression.Constant(42);

        var result = QueryTranslationEfCore<QueryProbe>.Unwrap(constant);

        result.Should().BeSameAs(constant);
    }
}
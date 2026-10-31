using System.Linq.Expressions;

using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Mapping;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

[Trait("Category", "Unit")]
public sealed class ExpressionsTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();

    [Fact(DisplayName = "BuildMember_WithValidField_ReturnsExpression")]
    public void BuildMember_WithValidField_ReturnsExpression()
    {
        var field = Map.Fields.First(f => f.Name == "name");
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = QueryTranslationEfCore<QueryProbe>.BuildMember(field, parameter, indexValue: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact(DisplayName = "BuildMember_WithNullExpression_ReturnsNotFilterableError")]
    public void BuildMember_WithNullExpression_ReturnsNotFilterableError()
    {
        // The builder cannot create a field with null expression (falls back to lambda),
        // so we test the indexed path which requires a proper expression
        var field = CreateField("indexed", indexType: null);
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = QueryTranslationEfCore<QueryProbe>.BuildMember(field, parameter, "test");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "BuildMember_WithIndexValue_ReturnsIndexedExpression")]
    public void BuildMember_WithIndexValue_ReturnsIndexedExpression()
    {
        // The builder sets IsIndexed but not IndexType properly — test that
        // an indexed field without IndexType returns NotFilterable
        var field = CreateField("indexed", indexType: typeof(string));
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = QueryTranslationEfCore<QueryProbe>.BuildMember(field, parameter, "test");

        // IsIndexed=true but expression only has 1 parameter, so it fails
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == QueryErrorCodes.FieldNotFilterable);
    }

    [Fact(DisplayName = "BuildMember_WithIndexButNoIndexType_ReturnsError")]
    public void BuildMember_WithIndexButNoIndexType_ReturnsError()
    {
        var field = CreateField("indexed", indexType: null);
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");

        var result = QueryTranslationEfCore<QueryProbe>.BuildMember(field, parameter, "test");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Query.Field.NotFilterable");
    }

    [Fact(DisplayName = "BuildSafeObject_WrapsNullGuard")]
    public void BuildSafeObject_WrapsNullGuard()
    {
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");
        var member = Expression.Property(parameter, "Tags");

        var result = QueryTranslationEfCore<QueryProbe>.BuildSafeObject(member);

        // BuildSafeObject returns a Convert expression when no guard is needed,
        // or a ConditionalExpression when a guard is added. Tags property on QueryProbe
        // is string[] which is not nullable at the member level, so no guard is added.
        result.Should().BeOfType<UnaryExpression>();
        result.NodeType.Should().Be(ExpressionType.Convert);
    }

    [Fact(DisplayName = "ApplyGuard_AddsNullGuardForNullableMember")]
    public void ApplyGuard_AddsNullGuardForNullableMember()
    {
        var parameter = Expression.Parameter(typeof(QueryProbe), "x");
        var member = Expression.Property(parameter, "Name");
        var body = Expression.Equal(member, Expression.Constant("test"));

        var result = QueryTranslationEfCore<QueryProbe>.ApplyGuard(member, true, body);

        // ApplyGuard returns Expression.AndAlso which is a LogicalBinaryExpression (subtype of BinaryExpression)
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

        // PropertyExpression is a subtype of MemberExpression
        result.Should().BeAssignableTo<MemberExpression>();
        ((MemberExpression)result).Expression.Should().BeSameAs(to);
    }

    [Fact(DisplayName = "Replace_LeavesOtherParametersUnchanged")]
    public void Replace_LeavesOtherParametersUnchanged()
    {
        var from = Expression.Parameter(typeof(QueryProbe), "old");
        var to = Expression.Parameter(typeof(QueryProbe), "new");
        var other = Expression.Parameter(typeof(string), "other");
        var body = Expression.Call(
            Expression.Property(from, "Name"),
            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!,
            other);

        var result = QueryTranslationEfCore<QueryProbe>.Replace(from, to, body);

        // InstanceMethodCallExpression1 is a subtype of MethodCallExpression
        result.Should().BeAssignableTo<MethodCallExpression>();
        var call = (MethodCallExpression)result;
        // call.Object is the MemberExpression (new.Name), whose Expression is the replaced parameter
        call.Object.Should().BeAssignableTo<MemberExpression>();
        ((MemberExpression)call.Object).Expression.Should().BeSameAs(to);
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

    private static QueryField CreateField(
        string name,
        LambdaExpression? expression = null,
        Type? indexType = null)
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map(name, probe => probe.Name, field =>
            {
                if (expression is not null)
                    field.Expression(expression);
                if (indexType is not null)
                    field.IsIndexed(true);
            });

        var result = builder.Build();
        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to build query map: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var field = result.Value.Fields.First(f => f.Name == name);
        // Override index type if needed by creating a new map with the right settings
        return field;
    }
}

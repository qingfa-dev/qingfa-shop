using System.Linq.Expressions;

using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Grouping;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Projection;
using Application.Common.Querying.Sorting;

namespace Application.Common.Querying.Expressions;

public static partial class QueryExpressionTranslator<T>
{
    public static Result<SortField> TranslateOrder(
        Expression<Func<T, object?>> selector,
        SortDirection direction,
        QueryMap<T> map)
    {
        Result<QueryField> field = ResolveMember(selector.Body, selector.Parameters[0], map);
        if (field.IsFailure)
            return Result<SortField>.Fail(field.Errors);

        if (!field.Value.Sortable)
            return Result<SortField>.Fail(QueryOutcome.Failure.NotSortable(field.Value.Name));

        return Result<SortField>.Ok(
            new SortField(field.Value.Name, direction, SortNulls.None));
    }

    public static Result<SelectField> TranslateSelect(
        Expression<Func<T, object?>> selector,
        QueryMap<T> map)
    {
        Result<QueryField> field = ResolveMember(selector.Body, selector.Parameters[0], map);
        if (field.IsFailure)
            return Result<SelectField>.Fail(field.Errors);

        if (!field.Value.Selectable)
            return Result<SelectField>.Fail(QueryOutcome.Failure.NotSelectable(field.Value.Name));

        return Result<SelectField>.Ok(new SelectField(field.Value.Name, field.Value.Name));
    }

    public static Result<IReadOnlyList<GroupByField>> TranslateGroup(
        Expression<Func<T, object?>> selector,
        QueryMap<T> map)
    {
        Expression body = Unwrap(selector.Body);
        IEnumerable<Expression> parts = body switch
        {
            NewExpression created => created.Arguments,
            MemberInitExpression initialized => initialized.Bindings
                .OfType<MemberAssignment>()
                .Select(binding => binding.Expression),
            _ => [body],
        };

        var keys = new List<GroupByField>();
        foreach (Expression part in parts)
        {
            Result<QueryField> field = ResolveMember(part, selector.Parameters[0], map);
            if (field.IsFailure)
                return Result<IReadOnlyList<GroupByField>>.Fail(field.Errors);

            if (!field.Value.Groupable)
                return Result<IReadOnlyList<GroupByField>>.Fail(
                    QueryOutcome.Failure.NotGroupable(field.Value.Name));

            if (keys.Any(key => key.Field == field.Value.Name))
                return Result<IReadOnlyList<GroupByField>>.Fail(
                    QueryOutcome.Failure.Duplicate(field.Value.Name));

            keys.Add(new GroupByField(field.Value.Name));
        }

        return Result<IReadOnlyList<GroupByField>>.Ok(keys);
    }

    public static Result<AggregateSpec> TranslateAggregate<TField>(
        AggregateFunction function,
        Expression<Func<T, TField>> selector,
        string? alias,
        QueryMap<T> map)
    {
        Result<QueryField> field = ResolveMember(selector.Body, selector.Parameters[0], map);
        if (field.IsFailure)
            return Result<AggregateSpec>.Fail(field.Errors);

        if (!field.Value.Selectable)
            return Result<AggregateSpec>.Fail(
                QueryOutcome.Failure.NotSelectable(field.Value.Name));

        if (!AggregateFunctionRule.IsSupported(function, field.Value.ComparisonType))
            return Result<AggregateSpec>.Fail(
                QueryOutcome.Failure.AggregateUnsupported(field.Value.Name));

        string resolvedAlias = alias
            ?? $"{function.ToToken()}({field.Value.Name})";

        return Result<AggregateSpec>.Ok(
            new AggregateSpec(function, field.Value.Name, resolvedAlias));
    }
}
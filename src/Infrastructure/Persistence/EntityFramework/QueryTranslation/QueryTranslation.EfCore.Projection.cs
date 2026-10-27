using System.Linq.Expressions;

using Application.Common.Querying;
using Application.Common.Querying.Mapping;
using Application.Common.Querying.Projection;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    /// <summary>
    /// Builds a server-side row projection for a select-only query.
    /// Each value is wrapped with BuildSafeObject because the heterogeneous row array
    /// (object[]) requires all values as object; value types must be boxed with null safety.
    /// </summary>
    public Result<TabularPlan> BuildSelectPlan(
        IQueryable<T> source,
        IReadOnlyList<SelectField> select,
        QueryMap<T> map)
    {
        // Validate: an empty projection has no columns to return.
        if (select.Count == 0)
            return Result<TabularPlan>.Fail(QueryOutcome.Failure.Syntax("no columns selected"));

        // Validate: resolve every column before building the expression tree so a
        // direct Query<T> call reports a neutral field error instead of a provider 500.
        var resolved = new List<(SelectField Selected, QueryField Field)>(select.Count);
        foreach (SelectField field in select)
        {
            Result<QueryField> mapped = ResolveSelectable(field.Field, map);
            if (mapped.IsFailure)
                return Result<TabularPlan>.Fail(mapped.Errors);

            resolved.Add((field, mapped.Value));
        }

        // Build: construct the object[] array expression for server-side projection.
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        var values = new List<Expression>(resolved.Count);
        foreach ((SelectField _, QueryField field) in resolved)
        {
            Result<Expression> member = BuildMember(field, parameter, indexValue: null);
            if (member.IsFailure)
                return Result<TabularPlan>.Fail(member.Errors);

            // Box: wrap value in object for heterogeneous row array; value types must be
            // boxed with null safety so nulls are preserved in the object[] result.
            values.Add(BuildSafeObject(member.Value));
        }

        // Assemble: create the array initializer and wrap it in a lambda for Select().
        var row = Expression.NewArrayInit(typeof(object), values.ToArray());
        var projection = Expression.Lambda<Func<T, object[]>>(row, parameter);
        TabularColumn[] columns = resolved
            .Select(entry => new TabularColumn(entry.Selected.Alias, entry.Field.ComparisonType))
            .ToArray();

        return Result<TabularPlan>.Success(new TabularPlan(source.Select(projection), columns));
    }
}
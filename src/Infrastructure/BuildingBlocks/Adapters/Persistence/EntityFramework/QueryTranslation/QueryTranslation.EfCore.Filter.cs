using System.Linq.Expressions;
using System.Reflection;

using Application.BuildingBlocks.Querying;
using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    /// <summary>Applies a filter tree to a source query.</summary>
    public Result<IQueryable<T>> ApplyFilter(
        IQueryable<T> source,
        FilterNode? filter,
        QueryMap<T> map)
    {
        // Skip: no filter means no WHERE clause — return the source unchanged.
        if (filter is null)
            return Result<IQueryable<T>>.Success(source);

        // Build: convert the filter AST into an Expression tree EF Core can translate.
        Result<Expression<Func<T, bool>>> predicate = BuildPredicate(filter, map);
        if (predicate.IsFailure)
            return Result<IQueryable<T>>.Fail(predicate.Errors);

        return Result<IQueryable<T>>.Success(source.Where(predicate.Value));
    }

    /// <summary>Recursively builds an Expression tree from a filter node, producing a lambda predicate.</summary>
    private Result<Expression<Func<T, bool>>> BuildPredicate(FilterNode node, QueryMap<T> map)
    {
        // Create: the parameter represents each entity row in the lambda.
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Result<Expression> body = BuildNode(node, parameter, map);
        if (body.IsFailure)
            return Result<Expression<Func<T, bool>>>.Fail(body.Errors);

        // Wrap: the body becomes a lambda so EF Core can translate it to SQL WHERE.
        return Result<Expression<Func<T, bool>>>.Success(
            Expression.Lambda<Func<T, bool>>(body.Value, parameter));
    }

    /// <summary>Dispatches a filter node to the appropriate builder: groups recurse, conditions build binary expressions.</summary>
    private Result<Expression> BuildNode(
        FilterNode node,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        // Dispatch: route each AST node to its builder — groups recurse, conditions build expressions.
        switch (node)
        {
            case FilterGroup group:
                return BuildGroup(group, parameter, map);

            case FilterCondition condition:
                return BuildCondition(condition, parameter, map);

            default:
                return Result<Expression>.Fail(
                    QueryOutcome.Failure.Syntax("unknown filter node"));
        }
    }

    /// <summary>
    /// Combines child expressions with AND or OR logic.
    /// Combined starts as null so the first child becomes the base expression;
    /// subsequent children are AND/OR'd into it, avoiding an unnecessary first-operand Not.
    /// </summary>
    private Result<Expression> BuildGroup(
        FilterGroup group,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        // Combine: start with null so the first child becomes the base expression;
        // subsequent children are AND/OR'd into it, avoiding an unnecessary first-operand Not.
        Expression? combined = null;

        foreach (FilterNode child in group.Children)
        {
            Result<Expression> built = BuildNode(child, parameter, map);
            if (built.IsFailure)
                return Result<Expression>.Fail(built.Errors);

            combined = combined is null
                ? built.Value
                : group.Logic == FilterLogic.And
                    ? Expression.AndAlso(combined, built.Value)
                    : Expression.OrElse(combined, built.Value);
        }

        // Validate: an empty group produces no expression — it's a malformed AST.
        return combined is null
            ? Result<Expression>.Fail(QueryOutcome.Failure.Syntax("empty filter group"))
            : Result<Expression>.Success(combined);
    }

    /// <summary>
    /// Builds a binary comparison expression for a single filter condition.
    /// Composite fields are handled separately: each sub-member produces its own comparison,
    /// then the results are OR'd together so a match on any sub-field satisfies the condition.
    /// </summary>
    private Result<Expression> BuildCondition(
        FilterCondition condition,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        // Resolve: look up the field and check filterability.
        Result<QueryField> field = Resolve(map, condition.Field, sorting: false);
        if (field.IsFailure)
            return Result<Expression>.Fail(field.Errors);

        // Merge: case-insensitive flag comes from the condition or the field's default.
        bool insensitive = condition.CaseInsensitive ||
            (field.Value.CaseInsensitive && field.Value.ComparisonType == typeof(string));
        bool needsLeaf = NeedsLeaf(condition);

        // Split: composite fields (e.g., "FullName") expand to multiple member expressions,
        // each compared separately and OR'd together.
        if (field.Value.Composite.Count > 0)
        {
            Expression[] members = field.Value.Composite
                .Select(member => Replace(member, parameter))
                .ToArray();

            return BuildComposite(field.Value, members, condition, insensitive, needsLeaf);
        }

        Result<Expression> member = BuildMember(field.Value, parameter, condition.IndexValue);
        if (member.IsFailure)
            return Result<Expression>.Fail(member.Errors);

        if (!field.Value.IsCollection)
        {
            // Scalar: a single member compared directly.
            Result<Expression> scalar = BuildBinary(member.Value, condition, insensitive);
            if (scalar.IsFailure)
                return scalar;

            return Result<Expression>.Success(
                ApplyGuard(member.Value, needsLeaf, scalar.Value));
        }

        // Collection: build an Any() call so EF Core translates "collection contains a matching element."
        Type elementType = field.Value.ElementType!;
        ParameterExpression element = Expression.Parameter(elementType, "e");
        Result<Expression> elementBody = BuildBinary(element, condition, insensitive);
        if (elementBody.IsFailure)
            return elementBody;

        Expression body = elementBody.Value;
        if (needsLeaf && NeedsElementGuard(elementType))
        {
            // Guard: reference element types get a null check; primitive collections
            // cannot translate `e != null` under EF Core's InMemory provider.
            body = Expression.AndAlso(NotNull(element), body);
        }

        // Fix: use the generic Any method with proper IEnumerable<TSource> type
        MethodInfo anyMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2);
        anyMethod = anyMethod.MakeGenericMethod(elementType);
        Expression any = Expression.Call(
            anyMethod,
            member.Value,
            Expression.Lambda(body, element));

        // Guard: a null collection must not be dereferenced by Any.
        return Result<Expression>.Success(
            ApplyGuard(CollectionSource(member.Value), includeLeaf: true, any));
    }

    /// <summary>Builds the OR chain a composite map expands a condition into.</summary>
    private Result<Expression> BuildComposite(
        QueryField field,
        IReadOnlyList<Expression> members,
        FilterCondition condition,
        bool caseInsensitive,
        bool needsLeaf)
    {
        // Combine: OR each sub-member so a match on any part satisfies the composite condition.
        Expression? combined = null;

        foreach (Expression member in members)
        {
            Result<Expression> body = BuildBinary(member, condition, caseInsensitive);
            if (body.IsFailure)
                return Result<Expression>.Fail(body.Errors);

            // Guard: each sub-member gets its own null guard.
            Expression guarded = ApplyGuard(member, needsLeaf, body.Value);
            combined = combined is null ? guarded : Expression.OrElse(combined, guarded);
        }

        return combined is null
            ? Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name))
            : Result<Expression>.Success(combined);
    }

    private Result<Expression> BuildBinary(
        Expression member,
        FilterCondition condition,
        bool caseInsensitive)
    {
        try
        {
            return BuildBinaryCore(member, condition, caseInsensitive);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or InvalidCastException)
        {
            // Translate: expression factories throw when an operator or operand does not fit
            // the member type (for example `<` on a string). The contract reports a value.
            return Result<Expression>.Fail(
                QueryOutcome.Failure.ValueInvalid(condition.Field, condition.RawValue ?? "null"));
        }
    }

    // Build: the core binary expression builder — handles empty values, nulls, enums, and operators.
    private Result<Expression> BuildBinaryCore(
        Expression member,
        FilterCondition condition,
        bool caseInsensitive)
    {
        // Handle: an empty string with Equal/NotEqual means "is empty" / "is not empty."
        if (condition.RawValue is { Length: 0 } && condition.Operator is
            FilterOperator.Equal or FilterOperator.NotEqual)
        {
            Expression empty = BuildEmpty(member);
            return Result<Expression>.Success(
                condition.Operator == FilterOperator.Equal ? empty : Expression.Not(empty));
        }

        // Handle: null values only work with equality operators.
        if (condition.Value is null)
        {
            // Validate: only equality operators accept a null operand.
            if (condition.Operator is not (FilterOperator.Equal or FilterOperator.NotEqual))
            {
                return Result<Expression>.Fail(
                    QueryOutcome.Failure.ValueInvalid(condition.Field, condition.RawValue ?? "null"));
            }

            // Validate: a non-nullable value type can never equal null.
            if (!IsNullable(member.Type))
            {
                return Result<Expression>.Fail(
                    QueryOutcome.Failure.ValueInvalid(condition.Field, condition.RawValue ?? "null"));
            }

            Expression isNull = Expression.Equal(member, Expression.Constant(null, member.Type));
            return Result<Expression>.Success(
                condition.Operator == FilterOperator.Equal ? isNull : Expression.Not(isNull));
        }

        // Parameterize: wrap the value so EF Core binds it as a query parameter for plan caching.
        Expression value = Parameterize(condition.Value, condition.Value.GetType());
        if (value.Type != member.Type)
            value = Expression.Convert(value, member.Type);

        // Translate: enums (and nullable enums) widen to their integer only for ordering;
        // equality keeps the enum type so a value converter on the column still applies.
        if (IsOrdering(condition.Operator) && EnumUnderlying(member.Type) is { } underlying)
        {
            Type ordered = member.Type.IsEnum ? underlying : typeof(Nullable<>).MakeGenericType(underlying);
            member = Expression.Convert(member, ordered);
            value = Expression.Convert(value, ordered);
        }

        // Transform: case-insensitive comparison uses ToLower on both sides.
        if (caseInsensitive && member.Type == typeof(string))
        {
            member = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
            value = Parameterize(((string)condition.Value).ToLowerInvariant(), typeof(string));
        }

        // Dispatch: build the binary expression for the operator.
        return condition.Operator switch
        {
            FilterOperator.Equal              => Result<Expression>.Success(Expression.Equal(member, value)),
            FilterOperator.NotEqual           => Result<Expression>.Success(Expression.NotEqual(member, value)),
            FilterOperator.LessThan           => Result<Expression>.Success(Expression.LessThan(member, value)),
            FilterOperator.GreaterThan        => Result<Expression>.Success(Expression.GreaterThan(member, value)),
            FilterOperator.GreaterThanOrEqual => Result<Expression>.Success(Expression.GreaterThanOrEqual(member, value)),
            FilterOperator.LessThanOrEqual    => Result<Expression>.Success(Expression.LessThanOrEqual(member, value)),
            FilterOperator.Contains           => Result<Expression>.Success(
                Expression.Call(member, nameof(string.Contains), Type.EmptyTypes, value)),
            FilterOperator.NotContains        => Result<Expression>.Success(
                Expression.Not(Expression.Call(member, nameof(string.Contains), Type.EmptyTypes, value))),
            FilterOperator.StartsWith         => Result<Expression>.Success(
                Expression.Call(member, nameof(string.StartsWith), Type.EmptyTypes, value)),
            FilterOperator.NotStartsWith      => Result<Expression>.Success(
                Expression.Not(Expression.Call(member, nameof(string.StartsWith), Type.EmptyTypes, value))),
            FilterOperator.EndsWith           => Result<Expression>.Success(
                Expression.Call(member, nameof(string.EndsWith), Type.EmptyTypes, value)),
            FilterOperator.NotEndsWith        => Result<Expression>.Success(
                Expression.Not(Expression.Call(member, nameof(string.EndsWith), Type.EmptyTypes, value))),
            FilterOperator.Custom             => condition.CustomOperator is { } custom
                ? _operators.Build(custom, member, value)
                : Result<Expression>.Fail(QueryOutcome.Failure.Syntax("custom operator has no name")),
            _ => Result<Expression>.Fail(QueryOutcome.Failure.Syntax("unknown operator")),
        };
    }

    // Check: a guard is needed when the value is not null and not an empty-string equality check.
    private static bool NeedsLeaf(FilterCondition condition) =>
        condition.Value is not null &&
        !(condition.RawValue is { Length: 0 } && condition.Operator is
            FilterOperator.Equal or FilterOperator.NotEqual);

    // Check: reference types need a null guard in collection Any(); scalar types don't.
    private static bool NeedsElementGuard(Type elementType) =>
        IsNullable(elementType) && !IsScalar(elementType);

    // Unwrap: peel through Select/Where calls to find the original collection member expression.
    private static Expression CollectionSource(Expression expression)
    {
        Expression current = Unwrap(expression);
        while (current is MethodCallExpression call &&
               call.Method.DeclaringType == typeof(Enumerable) &&
               call.Arguments.Count > 0)
        {
            // Walk: each Enumerable method's first argument is the source collection.
            current = Unwrap(call.Arguments[0]);
        }

        return current;
    }

    private static Expression BuildEmpty(Expression member)
    {
        // Validate: for strings "empty" is null or "" (default(string) is only null).
        if (member.Type == typeof(string))
        {
            return Expression.OrElse(
                Expression.Equal(member, Expression.Constant(null, typeof(string))),
                Expression.Equal(member, Expression.Constant(string.Empty)));
        }

        // Nullable types are empty when null; plain value types when default.
        return IsNullable(member.Type)
            ? Expression.Equal(member, Expression.Constant(null, member.Type))
            : Expression.Equal(member, Expression.Default(member.Type));
    }

    // Classify: ordering operators need enum-to-int widening for comparison.
    private static bool IsOrdering(FilterOperator op) =>
        op is FilterOperator.LessThan or FilterOperator.GreaterThan or
            FilterOperator.LessThanOrEqual or FilterOperator.GreaterThanOrEqual;
}
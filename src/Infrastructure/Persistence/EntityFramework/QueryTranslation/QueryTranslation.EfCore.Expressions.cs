using System.Linq.Expressions;

using Application.Common.Querying;
using Application.Common.Querying.Mapping;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    // ---- Null guards -----------------------------------------------------
    /// <summary>
    /// Wraps a body expression with null guards so null intermediates do not throw.
    /// Keeps null checks outside the operator so negations cannot match null rows.
    /// </summary>
    internal static Expression ApplyGuard(Expression member, bool includeLeaf, Expression body)
    {
        // Guard: keep null checks outside the operator so negations cannot match null rows.
        Expression? guard = BuildGuard(member, includeLeaf);
        // Combine: the guard must pass before the body is evaluated (short-circuit AND).
        return guard is null ? body : Expression.AndAlso(guard, body);
    }

    /// <summary>
    /// Builds a combined null-guard expression for all nullable parts of a member access chain.
    /// Returns null if no guards are needed (all parts are non-nullable).
    /// </summary>
    internal static Expression? BuildGuard(Expression member, bool includeLeaf)
    {
        // Collect: walk the member chain and gather null checks for each nullable link.
        List<Expression> guards = [];
        CollectGuards(Unwrap(member), includeLeaf, guards);
        if (guards.Count == 0)
            return null;

        // Combine: AND all guards together so every link must be non-null.
        Expression guard = guards[0];
        for (int index = 1; index < guards.Count; index++)
            guard = Expression.AndAlso(guard, guards[index]);

        return guard;
    }

    /// <summary>
    /// Walks the member access chain, collecting null checks for each intermediate navigation
    /// that can be null. Each intermediate reference property needs its own guard because
    /// EF Core will throw NullReferenceExpression at runtime if any link is null.
    /// </summary>
    private static void CollectGuards(Expression expression, bool includeLeaf, List<Expression> guards)
    {
        // Unwrap: remove Convert/ConvertChecked layers to reach the actual member access.
        expression = Unwrap(expression);

        switch (expression)
        {
            // Recurse: walk up the member chain, collecting guards for each intermediate link.
            case MemberExpression { Expression: not null } member:
                CollectGuards(member.Expression, includeLeaf: false, guards);
                AddOwnerGuard(member.Expression, guards);
                break;
            case IndexExpression { Object: not null } index:
                CollectGuards(index.Object, includeLeaf: false, guards);
                AddOwnerGuard(index.Object, guards);
                break;
        }

        // Leaf: if requested, add a null check for the final member itself.
        if (includeLeaf && expression is not ParameterExpression && IsNullable(expression.Type))
            guards.Add(NotNull(expression));
    }

    /// <summary>Adds a null guard for the owner expression if it is a nullable reference type.</summary>
    private static void AddOwnerGuard(Expression? owner, List<Expression> guards)
    {
        // Skip: no owner means we're at the root parameter.
        if (owner is null)
            return;

        Expression expression = Unwrap(owner);
        // Skip: the root parameter and non-nullable types don't need a guard.
        if (expression is ParameterExpression || !IsNullable(expression.Type))
            return;

        guards.Add(NotNull(expression));
    }

    /// <summary>Builds a not-equal-null expression for the given expression.</summary>
    private static Expression NotNull(Expression expression) =>
        Expression.NotEqual(expression, Expression.Constant(null, expression.Type));

    // ---- Type predicates -------------------------------------------------
    private static bool IsNullable(Type type) =>
        !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static bool IsScalar(Type type)
    {
        Type value = Nullable.GetUnderlyingType(type) ?? type;
        return value.IsPrimitive ||
            value.IsEnum ||
            value == typeof(string) ||
            value == typeof(decimal) ||
            value == typeof(Guid) ||
            value == typeof(DateTime) ||
            value == typeof(DateTimeOffset) ||
            value == typeof(TimeSpan);
    }

    // ---- Safe accessors -------------------------------------------------
    /// <summary>
    /// Wraps a member expression with a null guard and converts the result to object.
    /// Ensures that a null intermediate always produces null (not default(T) for value types),
    /// so aggregates are not polluted with zero values.
    /// </summary>
    internal static Expression BuildSafeObject(Expression member)
    {
        // Guard: build the conditional at object level so a null intermediate is
        // always null, even when the leaf is a value type (int would otherwise
        // become 0 and pollute aggregates).
        Expression value = Expression.Convert(member, typeof(object));
        Expression? guard = BuildGuard(member, includeLeaf: false);
        return guard is null
            ? value
            // Conditional: if the guard passes, box the value; otherwise return null.
            : Expression.Condition(guard, value, Expression.Constant(null, typeof(object)));
    }

    // ---- Operand construction -------------------------------------------
    /// <summary>
    /// Creates a parameterized operand by wrapping the value in a QueryValue holder.
    /// EF Core binds the holder as a query parameter for plan caching, rather than inlining
    /// a literal that would require a new plan for each distinct value.
    /// </summary>
    private static MemberExpression Parameterize(object? value, Type type)
    {
        // Cache: reading the operand off a holder lets EF Core bind it as a parameter
        // (one cached query plan) instead of inlining a literal per distinct value.
        object holder = Activator.CreateInstance(
            typeof(QueryValue<>).MakeGenericType(type), new object?[] { value })!;
        return Expression.Property(Expression.Constant(holder), nameof(QueryValue<int>.Value));
    }

    // Unwrap: nullable enums expose the underlying integer for ordering.
    private static Type? EnumUnderlying(Type type)
    {
        Type target = Nullable.GetUnderlyingType(type) ?? type;
        return target.IsEnum ? Enum.GetUnderlyingType(target) : null;
    }

    // ---- Member construction --------------------------------------------
    /// <summary>
    /// Builds an expression that accesses a mapped field on the entity parameter.
    /// Handles indexed fields by substituting the index value for the index parameter.
    /// </summary>
    internal static Result<Expression> BuildMember(
        QueryField field,
        ParameterExpression parameter,
        object? indexValue)
    {
        // Validate: the field must have an expression to build a member access.
        if (field.Expression is null)
            return Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name));

        // Replace: substitute the lambda's first parameter with the entity parameter.
        Expression body = Unwrap(field.Expression.Body);
        body = Replace(field.Expression.Parameters[0], parameter, body);

        if (indexValue is null)
        {
            // Validate: an indexed member without an index would leave a free parameter in the tree.
            return field.IsIndexed
                ? Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name))
                : Result<Expression>.Success(body);
        }

        // Validate: indexed fields must declare an index type.
        if (field.IndexType is null)
            return Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name));

        LambdaExpression expression = field.Expression;
        if (expression.Parameters.Count < 2)
            return Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name));

        // Substitute: replace the index parameter with the actual index value.
        return Result<Expression>.Success(Replace(
            expression.Parameters[1],
            Expression.Constant(indexValue, field.IndexType),
            body));
    }

    /// <summary>Replaces the first parameter of a lambda with a new parameter expression.</summary>
    // Replace: substitute the lambda's first parameter with a new parameter expression.
    private static Expression Replace(LambdaExpression lambda, ParameterExpression parameter) =>
        Replace(lambda.Parameters[0], parameter, Unwrap(lambda.Body));

    // Replace: walk the expression tree and swap every occurrence of the target parameter.
    internal static Expression Replace(ParameterExpression from, Expression to, Expression body) =>
        new ParameterReplacer(from, to).Visit(body)!;

    // Unwrap: recursively peel Convert/ConvertChecked layers to reach the underlying operand.
    internal static Expression Unwrap(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary
            ? Unwrap(unary.Operand)
            : expression;

    // Visitor: replace matching parameters during tree traversal.
    private sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
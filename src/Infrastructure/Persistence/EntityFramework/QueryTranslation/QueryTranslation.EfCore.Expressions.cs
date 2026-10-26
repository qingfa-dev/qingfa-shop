using System.Linq.Expressions;

using Application.Common.Querying;
using Application.Common.Querying.Mapping;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T>
{
    // ---- Null guards -----------------------------------------------------
    private static Expression ApplyGuard(Expression member, bool includeLeaf, Expression body)
    {
        // Guard: keep null checks outside the operator so negations cannot match null rows.
        Expression? guard = BuildGuard(member, includeLeaf);
        return guard is null ? body : Expression.AndAlso(guard, body);
    }

    private static Expression? BuildGuard(Expression member, bool includeLeaf)
    {
        List<Expression> guards = [];
        CollectGuards(Unwrap(member), includeLeaf, guards);
        if (guards.Count == 0)
            return null;

        Expression guard = guards[0];
        for (int index = 1; index < guards.Count; index++)
            guard = Expression.AndAlso(guard, guards[index]);

        return guard;
    }

    private static void CollectGuards(Expression expression, bool includeLeaf, List<Expression> guards)
    {
        expression = Unwrap(expression);

        switch (expression)
        {
            case MemberExpression { Expression: not null } member:
                CollectGuards(member.Expression, includeLeaf: false, guards);
                AddOwnerGuard(member.Expression, guards);
                break;
            case IndexExpression { Object: not null } index:
                CollectGuards(index.Object, includeLeaf: false, guards);
                AddOwnerGuard(index.Object, guards);
                break;
        }

        if (includeLeaf && expression is not ParameterExpression && IsNullable(expression.Type))
            guards.Add(NotNull(expression));
    }

    private static void AddOwnerGuard(Expression? owner, List<Expression> guards)
    {
        if (owner is null)
            return;

        Expression expression = Unwrap(owner);
        if (expression is ParameterExpression || !IsNullable(expression.Type))
            return;

        guards.Add(NotNull(expression));
    }

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
    private static Expression BuildSafeObject(Expression member)
    {
        // Guard: build the conditional at object level so a null intermediate is
        // always null, even when the leaf is a value type (int would otherwise
        // become 0 and pollute aggregates).
        Expression value = Expression.Convert(member, typeof(object));
        Expression? guard = BuildGuard(member, includeLeaf: false);
        return guard is null
            ? value
            : Expression.Condition(guard, value, Expression.Constant(null, typeof(object)));
    }

    // ---- Operand construction -------------------------------------------
    private static MemberExpression Parameterize(object? value, Type type)
    {
        // Translate: reading the operand off a holder lets EF Core bind it as a parameter
        // (one cached query plan) instead of inlining a literal per distinct value.
        object holder = Activator.CreateInstance(
            typeof(QueryValue<>).MakeGenericType(type), new object?[] { value })!;
        return Expression.Property(Expression.Constant(holder), nameof(QueryValue<int>.Value));
    }

    private static Type? EnumUnderlying(Type type)
    {
        Type target = Nullable.GetUnderlyingType(type) ?? type;
        return target.IsEnum ? Enum.GetUnderlyingType(target) : null;
    }

    // ---- Member construction --------------------------------------------
    private static Result<Expression> BuildMember(
        QueryField field,
        ParameterExpression parameter,
        object? indexValue)
    {
        if (field.Expression is null)
            return Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name));

        Expression body = Unwrap(field.Expression.Body);
        body = Replace(field.Expression.Parameters[0], parameter, body);

        if (indexValue is null)
        {
            // Validate: an indexed member without an index would leave a free parameter in the tree.
            return field.IsIndexed
                ? Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name))
                : Result<Expression>.Success(body);
        }

        if (field.IndexType is null)
            return Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name));

        LambdaExpression expression = field.Expression;
        if (expression.Parameters.Count < 2)
            return Result<Expression>.Fail(QueryOutcome.Failure.NotFilterable(field.Name));

        return Result<Expression>.Success(Replace(
            expression.Parameters[1],
            Expression.Constant(indexValue, field.IndexType),
            body));
    }

    private static Expression Replace(LambdaExpression lambda, ParameterExpression parameter) =>
        Replace(lambda.Parameters[0], parameter, Unwrap(lambda.Body));

    private static Expression Replace(ParameterExpression from, Expression to, Expression body) =>
        new ParameterReplacer(from, to).Visit(body)!;

    private static Expression Unwrap(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary
            ? Unwrap(unary.Operand)
            : expression;

    private sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
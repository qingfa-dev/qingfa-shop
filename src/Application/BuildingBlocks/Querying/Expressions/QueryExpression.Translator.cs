using System.Linq.Expressions;

using Application.BuildingBlocks.Querying.Filtering;
using Application.BuildingBlocks.Querying.Mapping;

namespace Application.BuildingBlocks.Querying.Expressions;

/// <summary>
/// Converts C# predicate expressions into the provider-neutral <see cref="FilterNode"/> tree,
/// resolving member accesses through a <see cref="QueryMap{T}"/>.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <remarks>
/// The translator never silently falls back: any expression shape it does not support
/// produces a typed <see cref="QueryOutcome.Failure.ExpressionUnsupported"/> error.
/// </remarks>
public static partial class QueryExpressionTranslator<T>
{
    /// <summary>Translates a boolean predicate into a filter tree, or a failure when unsupported.</summary>
    /// <param name="predicate">The predicate to translate.</param>
    /// <param name="map">The field map used to resolve member accesses.</param>
    /// <returns>The root filter node, or a typed failure.</returns>
    public static Result<FilterNode> TranslatePredicate(
        Expression<Func<T, bool>> predicate,
        QueryMap<T> map)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(map);

        return TranslateNode(predicate.Body, predicate.Parameters[0], map);
    }

    /// <summary>Recursively translates one predicate node into a filter node.</summary>
    /// <param name="expression">The node to translate.</param>
    /// <param name="parameter">The predicate parameter that represents the source entity.</param>
    /// <param name="map">The field map used to resolve member accesses.</param>
    /// <returns>The translated node, or a typed failure.</returns>
    private static Result<FilterNode> TranslateNode(
        Expression expression,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        expression = Unwrap(expression);
        return expression switch
        {
            BinaryExpression { NodeType: ExpressionType.AndAlso } binary =>
                Combine(binary, FilterLogic.And, parameter, map),
            BinaryExpression { NodeType: ExpressionType.OrElse } binary =>
                Combine(binary, FilterLogic.Or, parameter, map),
            BinaryExpression binary when IsComparison(binary.NodeType) =>
                TranslateComparison(binary, parameter, map),
            UnaryExpression { NodeType: ExpressionType.Not } unary =>
                TranslateNot(unary.Operand, parameter, map),
            MethodCallExpression call =>
                TranslateStringMethod(call, parameter, map),
            MemberExpression { Type: var type } member when type == typeof(bool) =>
                TranslateBooleanMember(member, parameter, map),
            _ => Result<FilterNode>.Fail(QueryOutcome.Failure.ExpressionUnsupported(
                $"Unsupported predicate node '{expression.NodeType}'.")),
        };
    }

    /// <summary>Resolves a member expression rooted at the selector parameter to its mapped field.</summary>
    /// <param name="expression">The member access to resolve.</param>
    /// <param name="root">The selector parameter the member chain must terminate at.</param>
    /// <param name="map">The field map to resolve against.</param>
    /// <returns>The mapped field, or a NotMapped/AmbiguousField/ExpressionUnsupported failure.</returns>
    /// <remarks>
    /// The root check keeps captured or static members whose names collide with mapped fields from
    /// being treated as entity fields. A member that does not terminate at <paramref name="root"/>
    /// fails instead of resolving by name.
    /// </remarks>
    internal static Result<QueryField> ResolveMember(
        Expression expression,
        ParameterExpression root,
        QueryMap<T> map)
    {
        expression = Unwrap(expression);
        if (expression is not MemberExpression member)
            return Result<QueryField>.Fail(
                QueryOutcome.Failure.ExpressionUnsupported("Expected a member access."));

        Expression? current = member;
        while (current is MemberExpression nested)
            current = nested.Expression is { } parent ? Unwrap(parent) : null;

        if (!ReferenceEquals(current, root))
            return Result<QueryField>.Fail(QueryOutcome.Failure.ExpressionUnsupported(
                "The member access must be rooted at the source entity."));

        return ResolveMember(member, map);
    }

    /// <summary>Resolves a member expression to its mapped field, or a typed failure.</summary>
    /// <param name="expression">The member access to resolve.</param>
    /// <param name="map">The field map to resolve against.</param>
    /// <returns>The mapped field, or a NotMapped/AmbiguousField/ExpressionUnsupported failure.</returns>
    /// <remarks>
    /// This overload does not check the member root. Public translators must use the
    /// parameter-aware overload so a captured member cannot masquerade as an entity field.
    /// </remarks>
    internal static Result<QueryField> ResolveMember(Expression expression, QueryMap<T> map)
    {
        expression = Unwrap(expression);
        if (expression is not MemberExpression member)
            return Result<QueryField>.Fail(
                QueryOutcome.Failure.ExpressionUnsupported("Expected a member access."));

        string path = MemberPath(member);
        var matches = map.Fields
            .Where(field => string.Equals(field.MemberPath, path, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            matches = map.Fields
                .Where(field => string.Equals(field.Name, path, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return matches.Count switch
        {
            0 => Result<QueryField>.Fail(QueryOutcome.Failure.NotMapped(path)),
            > 1 => Result<QueryField>.Fail(QueryOutcome.Failure.AmbiguousField(path)),
            _ => Result<QueryField>.Ok(matches[0]),
        };
    }

    /// <summary>Combines the left and right operands of a logical operator into a group.</summary>
    /// <param name="binary">The logical binary expression.</param>
    /// <param name="logic">How the two children combine.</param>
    /// <param name="parameter">The predicate parameter that represents the source entity.</param>
    /// <param name="map">The field map used to resolve member accesses.</param>
    /// <returns>A group containing both translated children, or the first failure.</returns>
    private static Result<FilterNode> Combine(
        BinaryExpression binary,
        FilterLogic logic,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        Result<FilterNode> left = TranslateNode(binary.Left, parameter, map);
        if (left.IsFailure)
            return left;

        Result<FilterNode> right = TranslateNode(binary.Right, parameter, map);
        if (right.IsFailure)
            return right;

        return Result<FilterNode>.Ok(new FilterGroup(logic, [left.Value, right.Value]));
    }

    /// <summary>Translates a comparison (==, !=, &lt;, &lt;=, &gt;, &gt;=) into a filter condition.</summary>
    /// <param name="binary">The comparison binary expression.</param>
    /// <param name="parameter">The predicate parameter that represents the source entity.</param>
    /// <param name="map">The field map used to resolve member accesses.</param>
    /// <returns>A condition, or a typed failure.</returns>
    private static Result<FilterNode> TranslateComparison(
        BinaryExpression binary,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        Expression left = Unwrap(binary.Left);
        Expression right = Unwrap(binary.Right);

        bool leftIsMember = IsMemberOf(left, parameter);
        bool rightIsMember = IsMemberOf(right, parameter);

        // Guard: a comparison must have exactly one member side and one constant side.
        if (leftIsMember == rightIsMember)
            return Result<FilterNode>.Fail(QueryOutcome.Failure.ExpressionUnsupported(
                "A comparison requires exactly one member operand."));

        Expression memberSide = leftIsMember ? left : right;
        Expression valueSide = leftIsMember ? right : left;

        Result<QueryField> field = ResolveMember(memberSide, parameter, map);
        if (field.IsFailure)
            return Result<FilterNode>.Fail(field.Errors);

        if (!field.Value.Filterable)
            return Result<FilterNode>.Fail(QueryOutcome.Failure.NotFilterable(field.Value.Name));

        // Guard: a value operand that still references the predicate parameter cannot be evaluated
        // and must fail typed instead of throwing from Expression.Compile.
        if (ReferencesParameter(valueSide, parameter))
            return Result<FilterNode>.Fail(QueryOutcome.Failure.ExpressionUnsupported(
                "The comparison value must not reference the source entity."));

        FilterOperator op = MapOperator(binary.NodeType, memberOnRight: rightIsMember);
        object? value = Evaluate(valueSide);

        return Result<FilterNode>.Ok(new FilterCondition(
            field.Value.Name,
            op,
            value?.ToString(),
            value,
            field.Value.ComparisonType,
            field.Value.CaseInsensitive,
            null,
            null));
    }

    /// <summary>Translates a logical negation by unwrapping nested ! and negating the inner node.</summary>
    /// <param name="operand">The operand of the outer Not expression.</param>
    /// <param name="parameter">The predicate parameter that represents the source entity.</param>
    /// <param name="map">The field map used to resolve member accesses.</param>
    /// <returns>The negated node, or a typed failure.</returns>
    private static Result<FilterNode> TranslateNot(
        Expression operand,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        operand = Unwrap(operand);

        // Normalize: the current negation and a nested one cancel, so translate the inner operand directly.
        if (operand is UnaryExpression { NodeType: ExpressionType.Not } nested)
            return TranslateNode(nested.Operand, parameter, map);

        // Map: !boolMember becomes an equality with false.
        if (operand is MemberExpression { Type: var type } boolMember && type == typeof(bool))
            return TranslateBooleanMember(boolMember, parameter, map, negate: true);

        Result<FilterNode> inner = TranslateNode(operand, parameter, map);
        if (inner.IsFailure)
            return inner;

        return Result<FilterNode>.Ok(Negate(inner.Value));
    }

    /// <summary>Translates a bare boolean member access into an equality with true (or false).</summary>
    /// <param name="member">The boolean member expression.</param>
    /// <param name="parameter">The predicate parameter that represents the source entity.</param>
    /// <param name="map">The field map used to resolve member accesses.</param>
    /// <param name="negate">When true, compare against false instead of true.</param>
    /// <returns>A condition, or a typed failure.</returns>
    private static Result<FilterNode> TranslateBooleanMember(
        MemberExpression member,
        ParameterExpression parameter,
        QueryMap<T> map,
        bool negate = false)
    {
        Result<QueryField> field = ResolveMember(member, parameter, map);
        if (field.IsFailure)
            return Result<FilterNode>.Fail(field.Errors);

        if (!field.Value.Filterable)
            return Result<FilterNode>.Fail(QueryOutcome.Failure.NotFilterable(field.Value.Name));

        bool value = !negate;
        return Result<FilterNode>.Ok(new FilterCondition(
            field.Value.Name,
            FilterOperator.Equal,
            value.ToString(),
            value,
            field.Value.ComparisonType,
            field.Value.CaseInsensitive,
            null,
            null));
    }

    /// <summary>Translates string.Contains, StartsWith, and EndsWith into filter conditions.</summary>
    /// <param name="call">The method call expression.</param>
    /// <param name="parameter">The predicate parameter that represents the source entity.</param>
    /// <param name="map">The field map used to resolve member accesses.</param>
    /// <returns>A condition, or a typed failure.</returns>
    private static Result<FilterNode> TranslateStringMethod(
        MethodCallExpression call,
        ParameterExpression parameter,
        QueryMap<T> map)
    {
        // Validate: only the three string instance methods with one string argument are supported.
        FilterOperator? op = call.Method.Name switch
        {
            nameof(string.Contains) => FilterOperator.Contains,
            nameof(string.StartsWith) => FilterOperator.StartsWith,
            nameof(string.EndsWith) => FilterOperator.EndsWith,
            _ => null,
        };

        if (op is null
            || call.Method.DeclaringType != typeof(string)
            || call.Object is null
            || call.Arguments.Count != 1
            || call.Arguments[0].Type != typeof(string))
        {
            return Result<FilterNode>.Fail(QueryOutcome.Failure.ExpressionUnsupported(
                $"Unsupported string method '{call.Method.Name}'."));
        }

        Result<QueryField> field = ResolveMember(call.Object, parameter, map);
        if (field.IsFailure)
            return Result<FilterNode>.Fail(field.Errors);

        if (!field.Value.Filterable)
            return Result<FilterNode>.Fail(QueryOutcome.Failure.NotFilterable(field.Value.Name));

        Expression argument = Unwrap(call.Arguments[0]);

        // Guard: an argument that reads the source entity cannot be evaluated outside the query.
        if (ReferencesParameter(argument, parameter))
            return Result<FilterNode>.Fail(QueryOutcome.Failure.ExpressionUnsupported(
                "The string method argument must not reference the source entity."));

        object? value = Evaluate(argument);

        return Result<FilterNode>.Ok(new FilterCondition(
            field.Value.Name,
            op.Value,
            value?.ToString(),
            value,
            field.Value.ComparisonType,
            field.Value.CaseInsensitive,
            null,
            null));
    }

    /// <summary>Builds the dotted member path for a member expression (e.g., "Child.Name").</summary>
    /// <param name="member">The member expression to walk.</param>
    /// <returns>The dotted path, or the single member name for a simple access.</returns>
    private static string MemberPath(MemberExpression member)
    {
        var parts = new Stack<string>();
        Expression? current = member;
        while (current is MemberExpression nested)
        {
            parts.Push(nested.Member.Name);
            current = nested.Expression;
        }

        return string.Join(QueryConstant.Tokens.Separator.Member, parts);
    }

    /// <summary>Evaluates a constant expression to its boxed runtime value.</summary>
    /// <param name="expression">The constant expression to evaluate.</param>
    /// <returns>The value, or null.</returns>
    private static object? Evaluate(Expression expression)
    {
        Expression value = Unwrap(expression);
        return Expression.Lambda<Func<object?>>(
            Expression.Convert(value, typeof(object))).Compile().Invoke();
    }

    /// <summary>Determines whether an expression tree references the predicate parameter.</summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="parameter">The predicate parameter to look for.</param>
    /// <returns>True when the expression reads the predicate parameter.</returns>
    private static bool ReferencesParameter(Expression expression, ParameterExpression parameter)
    {
        var visitor = new ParameterReferenceVisitor(parameter);
        visitor.Visit(expression);
        return visitor.Found;
    }

    /// <summary>Records whether an expression tree reads a specific parameter.</summary>
    private sealed class ParameterReferenceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        internal ParameterReferenceVisitor(ParameterExpression parameter) => _parameter = parameter;

        internal bool Found { get; private set; }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (ReferenceEquals(node, _parameter))
                Found = true;

            return base.VisitParameter(node);
        }
    }

    /// <summary>Strips boxing conversions so the underlying expression shape is visible.</summary>
    /// <param name="expression">The expression to unwrap.</param>
    /// <returns>The expression with leading Convert/ConvertChecked nodes removed.</returns>
    private static Expression Unwrap(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary
            ? Unwrap(unary.Operand)
            : expression;

    /// <summary>Determines whether an expression is a member access rooted at the parameter.</summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="parameter">The predicate parameter that represents the source entity.</param>
    /// <returns>True when the expression reads a member of the source entity.</returns>
    private static bool IsMemberOf(Expression expression, ParameterExpression parameter)
    {
        Expression? root = Unwrap(expression);
        if (root is not MemberExpression member)
            return false;

        root = member;
        while (root is MemberExpression nested)
            root = nested.Expression;

        return ReferenceEquals(root, parameter);
    }

    /// <summary>Determines whether a node type is one of the six comparison operators.</summary>
    /// <param name="nodeType">The node type to test.</param>
    /// <returns>True when the node type is a comparison.</returns>
    private static bool IsComparison(ExpressionType nodeType) => nodeType is
        ExpressionType.Equal or
        ExpressionType.NotEqual or
        ExpressionType.GreaterThan or
        ExpressionType.GreaterThanOrEqual or
        ExpressionType.LessThan or
        ExpressionType.LessThanOrEqual;

    /// <summary>Maps a comparison expression type to its filter operator, flipping relational operators when the member is on the right.</summary>
    /// <param name="nodeType">The comparison node type.</param>
    /// <param name="memberOnRight">True when the member is the right operand.</param>
    /// <returns>The mapped filter operator.</returns>
    private static FilterOperator MapOperator(ExpressionType nodeType, bool memberOnRight) =>
        nodeType switch
        {
            ExpressionType.Equal => FilterOperator.Equal,
            ExpressionType.NotEqual => FilterOperator.NotEqual,
            ExpressionType.GreaterThan => memberOnRight ? FilterOperator.LessThan : FilterOperator.GreaterThan,
            ExpressionType.GreaterThanOrEqual => memberOnRight ? FilterOperator.LessThanOrEqual : FilterOperator.GreaterThanOrEqual,
            ExpressionType.LessThan => memberOnRight ? FilterOperator.GreaterThan : FilterOperator.LessThan,
            ExpressionType.LessThanOrEqual => memberOnRight ? FilterOperator.GreaterThanOrEqual : FilterOperator.LessThanOrEqual,
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Not a comparison operator."),
        };

    /// <summary>Negates a filter node, applying De Morgan's laws to groups.</summary>
    /// <param name="node">The node to negate.</param>
    /// <returns>The negated node.</returns>
    private static FilterNode Negate(FilterNode node) => node switch
    {
        FilterCondition condition => condition with { Operator = Negate(condition.Operator) },
        FilterGroup group => new FilterGroup(
            Invert(group.Logic),
            group.Children.Select(Negate).ToArray()),
        _ => node,
    };

    /// <summary>Negates a filter operator (Equal &lt;-&gt; NotEqual, Contains &lt;-&gt; NotContains, etc.).</summary>
    /// <param name="op">The operator to negate.</param>
    /// <returns>The negated operator.</returns>
    private static FilterOperator Negate(FilterOperator op) => op switch
    {
        FilterOperator.Equal => FilterOperator.NotEqual,
        FilterOperator.NotEqual => FilterOperator.Equal,
        FilterOperator.Contains => FilterOperator.NotContains,
        FilterOperator.NotContains => FilterOperator.Contains,
        FilterOperator.StartsWith => FilterOperator.NotStartsWith,
        FilterOperator.NotStartsWith => FilterOperator.StartsWith,
        FilterOperator.EndsWith => FilterOperator.NotEndsWith,
        FilterOperator.NotEndsWith => FilterOperator.EndsWith,
        FilterOperator.LessThan => FilterOperator.GreaterThanOrEqual,
        FilterOperator.GreaterThan => FilterOperator.LessThanOrEqual,
        FilterOperator.LessThanOrEqual => FilterOperator.GreaterThan,
        FilterOperator.GreaterThanOrEqual => FilterOperator.LessThan,
        _ => op,
    };

    /// <summary>Inverts a group logic (And &lt;-&gt; Or).</summary>
    /// <param name="logic">The logic to invert.</param>
    /// <returns>The inverted logic.</returns>
    private static FilterLogic Invert(FilterLogic logic) => logic switch
    {
        FilterLogic.And => FilterLogic.Or,
        FilterLogic.Or => FilterLogic.And,
        _ => logic,
    };
}
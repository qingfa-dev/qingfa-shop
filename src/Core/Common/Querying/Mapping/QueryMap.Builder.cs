using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

namespace Core.Common.Querying.Mapping;

/// <summary>Builds a <see cref="QueryMap{T}"/> and reports every broken declaration.</summary>
/// <remarks>
/// A declaration is checked as it is made, and a broken one is recorded rather
/// than thrown, so one build reports every problem instead of only the first.
/// The builder is never handed out: <see cref="Build"/> produces the map, and it
/// produces one only when nothing was recorded.
/// </remarks>
public sealed class QueryMapBuilder<T>
{
    private readonly Dictionary<string, QueryField> _fields = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Error> _errors = [];

    private QueryMapBuilder()
    {
    }

    /// <summary>Starts a new map.</summary>
    public static QueryMapBuilder<T> Create() => new();

    /// <summary>Maps a member the query may address.</summary>
    /// <param name="name">The name the query addresses the member by.</param>
    /// <param name="member">The member to expose.</param>
    /// <param name="configure">Optional configuration for the field.</param>
    /// <returns>The builder, for chaining.</returns>
    public QueryMapBuilder<T> Map(
        string name,
        Expression<Func<T, object?>> member,
        Action<QueryFieldBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(member);
        if (!ValidateName(name))
            return this;

        var builder = new QueryFieldBuilder();
        configure?.Invoke(builder);
        Type valueType = ValueTypeOf(member);
        Type? elementType = ElementTypeOf(valueType);
        Add(builder.Build(name.Trim(), member, MemberPathOf(member), valueType, elementType, indexType: null));
        return this;
    }

    /// <summary>Maps one name over several members, matched by any of them.</summary>
    /// <param name="name">The name the query addresses the members by.</param>
    /// <param name="members">The members to expose.</param>
    /// <param name="configure">Optional configuration for the field.</param>
    /// <returns>The builder, for chaining.</returns>
    public QueryMapBuilder<T> MapComposite(
        string name,
        IReadOnlyList<Expression<Func<T, object?>>> members,
        Action<QueryFieldBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (!ValidateName(name))
            return this;

        if (members.Count == 0)
        {
            _errors.Add(QueryMapOutcome.Failure.CompositeEmpty);
            return this;
        }

        Type[] types = [.. members.Select(ValueTypeOf).Distinct()];
        if (types.Length != 1)
        {
            _errors.Add(QueryMapOutcome.Failure.CompositeTypeMismatch);
            return this;
        }

        var builder = new QueryFieldBuilder();
        configure?.Invoke(builder);
        builder.Sortable(false).Selectable(false).Groupable(false);
        Add(builder.BuildComposite(
            name.Trim(),
            members.Cast<LambdaExpression>().ToArray(),
            types[0],
            ElementTypeOf(types[0])));
        return this;
    }

    /// <summary>Maps a member the query addresses by an index.</summary>
    /// <typeparam name="TKey">The type of the index.</typeparam>
    /// <param name="name">The name the query addresses the member by.</param>
    /// <param name="member">The indexed member to expose.</param>
    /// <param name="configure">Optional configuration for the field.</param>
    /// <returns>The builder, for chaining.</returns>
    public QueryMapBuilder<T> MapIndexed<TKey>(
        string name,
        Expression<Func<T, TKey, object?>> member,
        Action<QueryFieldBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(member);
        if (!ValidateName(name))
            return this;

        var builder = new QueryFieldBuilder();
        configure?.Invoke(builder);
        builder.Sortable(false).Selectable(false).Groupable(false);
        Type valueType = ValueTypeOf(member);
        Add(builder.Build(
            name.Trim(),
            member,
            memberPath: null,
            valueType,
            elementType: null,
            indexType: typeof(TKey)));
        return this;
    }

    /// <summary>Maps every public property, optionally recursing into nested ones.</summary>
    /// <param name="depth">How many levels of nested properties to map.</param>
    /// <returns>The builder, for chaining.</returns>
    public QueryMapBuilder<T> GenerateMappings(int depth = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        Generate(typeof(T), prefix: null, depth);
        return this;
    }

    /// <summary>Produces the map, or every failure the declarations produced.</summary>
    /// <returns>
    /// The map when every declaration was sound; otherwise the failures, so the
    /// caller can report all of them at once.
    /// </returns>
    public Result<QueryMap<T>> Build() =>
        _errors.Count > 0
            ? Result<QueryMap<T>>.Fail(_errors)
            : Result<QueryMap<T>>.Success(new QueryMap<T>(_fields));

    private void Generate(Type type, string? prefix, int depth)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetMethod is null)
                continue;

            string name = prefix is null
                ? property.Name
                : $"{prefix}{QueryConstant.Tokens.Separator.Member}{property.Name}";
            if (_fields.ContainsKey(name))
                continue;

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            Expression access = parameter;
            foreach (string part in name.Split(QueryConstant.Tokens.Separator.Member))
                access = Expression.Property(access, part);

            var lambda = Expression.Lambda<Func<T, object?>>(
                Expression.Convert(access, typeof(object)), parameter);
            var builder = new QueryFieldBuilder();
            if (property.PropertyType == typeof(string))
                builder.Searchable();

            Add(builder.Build(
                name,
                lambda,
                MemberPathOf(lambda),
                property.PropertyType,
                ElementTypeOf(property.PropertyType),
                indexType: null));

            if (depth > 0 && IsNestable(property.PropertyType))
                Generate(property.PropertyType, name, depth - 1);
        }
    }

    private void Add(QueryField field)
    {
        if (field.Searchable && (field.ValueType != typeof(string) || field.Composite.Count > 0))
        {
            _errors.Add(QueryMapOutcome.Failure.SearchableNotString(field.Name));
            return;
        }

        if (!_fields.TryAdd(field.Name, field))
            _errors.Add(QueryMapOutcome.Failure.Duplicate(field.Name));
    }

    private bool ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _errors.Add(QueryMapOutcome.Failure.NameRequired);
            return false;
        }

        if (name.Trim().Length > QueryConstant.Limits.Map.MaxFieldNameLength)
        {
            _errors.Add(QueryMapOutcome.Failure.NameTooLong(QueryConstant.Limits.Map.MaxFieldNameLength));
            return false;
        }

        return true;
    }

    private static Type ValueTypeOf(LambdaExpression expression) => Unwrap(expression.Body).Type;

    private static Type? ElementTypeOf(Type type)
    {
        if (type == typeof(string))
            return null;

        Type? enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0];
    }

    private static Type? ElementTypeOf(LambdaExpression expression) => ElementTypeOf(ValueTypeOf(expression));

    private static string? MemberPathOf(LambdaExpression expression)
    {
        Expression body = Unwrap(expression.Body);
        var parts = new Stack<string>();
        while (body is MemberExpression member)
        {
            parts.Push(member.Member.Name);
            body = Unwrap(member.Expression!);
        }

        return body is ParameterExpression
            ? string.Join(QueryConstant.Tokens.Separator.Member, parts)
            : null;
    }

    private static Expression Unwrap(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary
            ? Unwrap(unary.Operand)
            : expression;

    private static bool IsNestable(Type type) =>
        type.IsClass &&
        type != typeof(string) &&
        !typeof(System.Collections.IEnumerable).IsAssignableFrom(type) &&
        !type.IsAbstract;
}
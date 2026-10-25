using System.Linq.Expressions;

namespace Core.Common.Querying.Mapping;

/// <summary>Immutable metadata for one exposed query field.</summary>
public sealed class QueryField
{
    internal QueryField(
        string name,
        Type valueType,
        Type? elementType,
        string? memberPath,
        LambdaExpression? expression,
        IReadOnlyList<LambdaExpression> composite,
        Type? indexType,
        QueryValueConverter? converter,
        bool filterable,
        bool sortable,
        bool searchable,
        bool selectable,
        bool groupable,
        bool caseInsensitive,
        bool allowNullSearch)
    {
        Name = name;
        ValueType = valueType;
        ElementType = elementType;
        MemberPath = memberPath;
        Expression = expression;
        Composite = composite;
        IndexType = indexType;
        Converter = converter;
        Filterable = filterable;
        Sortable = sortable;
        Searchable = searchable;
        Selectable = selectable;
        Groupable = groupable;
        CaseInsensitive = caseInsensitive;
        AllowNullSearch = allowNullSearch;
    }

    public string Name { get; }
    public Type ValueType { get; }
    public Type? ElementType { get; }
    public string? MemberPath { get; }
    public LambdaExpression? Expression { get; }
    public IReadOnlyList<LambdaExpression> Composite { get; }
    public Type? IndexType { get; }
    public QueryValueConverter? Converter { get; }
    public bool Filterable { get; }
    public bool Sortable { get; }
    public bool Searchable { get; }
    public bool Selectable { get; }
    public bool Groupable { get; }
    public bool CaseInsensitive { get; }
    public bool AllowNullSearch { get; }

    /// <summary>Gets a value indicating whether the member yields a sequence.</summary>
    public bool IsCollection => ElementType is not null;

    /// <summary>Gets a value indicating whether the member is addressed by an index.</summary>
    public bool IsIndexed => IndexType is not null;

    /// <summary>Gets the type used for comparisons; the element type for collections.</summary>
    public Type ComparisonType => ElementType ?? ValueType;
}
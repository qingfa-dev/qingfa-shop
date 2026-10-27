using System.Linq.Expressions;

namespace Application.Common.Querying.Mapping;

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

    /// <summary>The public name used in query strings (e.g., "Status", "Owner.Name").</summary>
    public string Name { get; }

    /// <summary>The .NET type of the member (e.g., typeof(string), typeof(int)).</summary>
    public Type ValueType { get; }

    /// <summary>For collections, the element type (e.g., typeof(string) for IEnumerable&lt;string&gt;); null for scalar fields.</summary>
    public Type? ElementType { get; }

    /// <summary>Dotted path to the member for navigation (e.g., "Owner.Name"); null for simple properties.</summary>
    public string? MemberPath { get; }

    /// <summary>The lambda expression that reads the member value from the entity.</summary>
    public LambdaExpression? Expression { get; }

    /// <summary>For composite fields, the list of sub-field expressions; empty for scalar fields.</summary>
    public IReadOnlyList<LambdaExpression> Composite { get; }

    /// <summary>For indexed fields, the type of the index parameter (e.g., typeof(int)); null for non-indexed fields.</summary>
    public Type? IndexType { get; }

    /// <summary>Optional converter that transforms the raw string value to the member type.</summary>
    public QueryValueConverter? Converter { get; }

    /// <summary>True when the field can be used in filter expressions.</summary>
    public bool Filterable { get; }

    /// <summary>True when the field can be used in ordering expressions.</summary>
    public bool Sortable { get; }

    /// <summary>True when the field can be searched with full-text or basic search.</summary>
    public bool Searchable { get; }

    /// <summary>True when the field can be projected in a select clause.</summary>
    public bool Selectable { get; }

    /// <summary>True when the field can be used as a group-by key.</summary>
    public bool Groupable { get; }

    /// <summary>True when string comparisons on this field should ignore case.</summary>
    public bool CaseInsensitive { get; }

    /// <summary>True when the field accepts the "null" keyword as a search value.</summary>
    public bool AllowNullSearch { get; }

    /// <summary>Gets a value indicating whether the member yields a sequence (collection field).</summary>
    public bool IsCollection => ElementType is not null;

    /// <summary>Gets a value indicating whether the member is addressed by an index (e.g., "Tags[0]").</summary>
    public bool IsIndexed => IndexType is not null;

    /// <summary>Gets the type used for comparisons; for collections this is the element type, otherwise the value type.</summary>
    public Type ComparisonType => ElementType ?? ValueType;
}
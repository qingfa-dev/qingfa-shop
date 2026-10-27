using System.Linq.Expressions;

namespace Application.Common.Querying.Mapping;

/// <summary>Configures one query field before it is frozen into a <see cref="QueryField"/>.</summary>
public sealed class QueryFieldBuilder
{
    private bool _filterable = true;
    private bool _sortable = true;
    private bool _selectable = true;
    private bool _searchable;
    private bool _groupable;
    private bool _caseInsensitive;
    private bool _allowNullSearch = true;
    private QueryValueConverter? _converter;
    private bool? _isCollection;
    private bool? _isIndexed;
    private string? _memberPath;
    private LambdaExpression? _expressionOverride;
    private readonly List<LambdaExpression> _compositeMembers = new();
    private Type? _elementTypeOverride;

    /// <summary>Enable or disable filtering on this field (enabled by default).</summary>
    /// <param name="value">Whether the field can be used in filter expressions.</param>
    public QueryFieldBuilder Filterable(bool value = true) { _filterable = value; return this; }

    /// <summary>Enable or disable sorting on this field (enabled by default).</summary>
    /// <param name="value">Whether the field can be used in ordering expressions.</param>
    public QueryFieldBuilder Sortable(bool value = true) { _sortable = value; return this; }

    /// <summary>Enable or disable projection on this field (enabled by default).</summary>
    /// <param name="value">Whether the field can be used in select clauses.</param>
    public QueryFieldBuilder Selectable(bool value = true) { _selectable = value; return this; }

    /// <summary>Enable or disable search on this field (disabled by default).</summary>
    /// <param name="value">Whether the field can be searched.</param>
    public QueryFieldBuilder Searchable(bool value = true) { _searchable = value; return this; }

    /// <summary>Enable or disable grouping on this field (disabled by default).</summary>
    /// <param name="value">Whether the field can be used as a group-by key.</param>
    public QueryFieldBuilder Groupable(bool value = true) { _groupable = value; return this; }

    /// <summary>Enable or disable case-insensitive comparison on this field (sensitive by default).</summary>
    /// <param name="value">Whether string comparisons should ignore case.</param>
    public QueryFieldBuilder CaseInsensitive(bool value = true) { _caseInsensitive = value; return this; }

    /// <summary>Enable or disable the "null" keyword as a search value (enabled by default).</summary>
    /// <param name="value">Whether the field accepts null as a search value.</param>
    public QueryFieldBuilder AllowNullSearch(bool value = true) { _allowNullSearch = value; return this; }

    /// <summary>Registers a custom value converter for this field.</summary>
    /// <param name="converter">The converter that transforms raw string values to the member type.</param>
    public QueryFieldBuilder ConvertWith(QueryValueConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        return this;
    }

    /// <summary>Overrides whether this field represents a collection.</summary>
    /// <param name="value">True when the field is a collection type.</param>
    public QueryFieldBuilder IsCollection(bool value) { _isCollection = value; return this; }

    /// <summary>Overrides whether this field is addressed by an index.</summary>
    /// <param name="value">True when the field uses index syntax (e.g., "Tags[0]").</param>
    public QueryFieldBuilder IsIndexed(bool value) { _isIndexed = value; return this; }

    /// <summary>Overrides the member path for this field.</summary>
    /// <param name="path">The dotted navigation path (e.g., "Owner.Name").</param>
    public QueryFieldBuilder MemberPath(string? path) { _memberPath = path; return this; }

    /// <summary>Overrides the expression for this field.</summary>
    /// <param name="expression">The lambda expression that reads the member value.</param>
    public QueryFieldBuilder Expression(LambdaExpression? expression) { _expressionOverride = expression; return this; }

    /// <summary>Adds a composite member to this field.</summary>
    /// <typeparam name="TOwner">The entity type the member belongs to.</typeparam>
    /// <param name="member">Lambda expression that reads the sub-field value.</param>
    public QueryFieldBuilder Composite<TOwner>(Expression<Func<TOwner, object?>> member)
    {
        _compositeMembers.Add(member);
        return this;
    }

    /// <summary>Overrides the element type for this field.</summary>
    /// <param name="elementType">The element type for collections, or null for scalar fields.</param>
    public QueryFieldBuilder ElementType(Type? elementType) { _elementTypeOverride = elementType; return this; }

    /// <summary>
    /// Freezes the builder configuration into an immutable <see cref="QueryField"/>.
    /// </summary>
    /// <param name="name">The public field name.</param>
    /// <param name="expression">The auto-detected member expression.</param>
    /// <param name="memberPath">The auto-detected member path.</param>
    /// <param name="valueType">The .NET type of the member.</param>
    /// <param name="elementType">The auto-detected element type for collections.</param>
    /// <param name="indexType">The auto-detected index type for indexed fields.</param>
    internal QueryField Build(
        string name,
        LambdaExpression expression,
        string? memberPath,
        Type valueType,
        Type? elementType,
        Type? indexType)
    {
        // Guard: test overrides take precedence so unit tests can simulate collection/indexed fields
        // without relying on reflection to detect the actual member type.
        if (_isCollection.HasValue && !_isCollection.Value)
            elementType = null;
        if (_isIndexed.HasValue && _isIndexed.Value)
            indexType = typeof(int);

        if (_memberPath is not null)
            memberPath = _memberPath;

        LambdaExpression? finalExpression = _expressionOverride ?? expression;

        if (_compositeMembers.Count > 0)
        {
            return new QueryField(name, valueType, _elementTypeOverride ?? elementType, memberPath, finalExpression,
                _compositeMembers, indexType, _converter,
                _filterable, _sortable, _searchable, _selectable, _groupable, _caseInsensitive, _allowNullSearch);
        }

        return new QueryField(name, valueType, _elementTypeOverride ?? elementType, memberPath, finalExpression,
            [], indexType, _converter,
            _filterable, _sortable, _searchable, _selectable, _groupable, _caseInsensitive, _allowNullSearch);
    }

    /// <summary>Builds a composite query field from multiple member expressions.</summary>
    /// <param name="name">The public field name.</param>
    /// <param name="members">The list of sub-field expressions.</param>
    /// <param name="valueType">The .NET type of the composite field.</param>
    /// <param name="elementType">The element type if the composite is a collection.</param>
    internal QueryField BuildComposite(
        string name,
        IReadOnlyList<LambdaExpression> members,
        Type valueType,
        Type? elementType) =>
        new(name, valueType, elementType, null, null, members, null, _converter,
            _filterable, _sortable, _searchable, _selectable, _groupable, _caseInsensitive, _allowNullSearch);
}
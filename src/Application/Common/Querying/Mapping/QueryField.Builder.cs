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

    public QueryFieldBuilder Filterable(bool value = true) { _filterable = value; return this; }
    public QueryFieldBuilder Sortable(bool value = true) { _sortable = value; return this; }
    public QueryFieldBuilder Selectable(bool value = true) { _selectable = value; return this; }
    public QueryFieldBuilder Searchable(bool value = true) { _searchable = value; return this; }
    public QueryFieldBuilder Groupable(bool value = true) { _groupable = value; return this; }
    public QueryFieldBuilder CaseInsensitive(bool value = true) { _caseInsensitive = value; return this; }
    public QueryFieldBuilder AllowNullSearch(bool value = true) { _allowNullSearch = value; return this; }
    public QueryFieldBuilder ConvertWith(QueryValueConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        return this;
    }

    /// <summary>Overrides whether this field represents a collection.</summary>
    public QueryFieldBuilder IsCollection(bool value) { _isCollection = value; return this; }

    /// <summary>Overrides whether this field is addressed by an index.</summary>
    public QueryFieldBuilder IsIndexed(bool value) { _isIndexed = value; return this; }

    /// <summary>Overrides the member path for this field.</summary>
    public QueryFieldBuilder MemberPath(string? path) { _memberPath = path; return this; }

    /// <summary>Overrides the expression for this field.</summary>
    public QueryFieldBuilder Expression(LambdaExpression? expression) { _expressionOverride = expression; return this; }

    /// <summary>Adds a composite member to this field.</summary>
    public QueryFieldBuilder Composite<TOwner>(Expression<Func<TOwner, object?>> member)
    {
        _compositeMembers.Add(member);
        return this;
    }

    /// <summary>Overrides the element type for this field.</summary>
    public QueryFieldBuilder ElementType(Type? elementType) { _elementTypeOverride = elementType; return this; }

    internal QueryField Build(
        string name,
        LambdaExpression expression,
        string? memberPath,
        Type valueType,
        Type? elementType,
        Type? indexType)
    {
        // Allow test overrides for collection/indexed state.
        if (_isCollection.HasValue && !_isCollection.Value)
            elementType = null;
        if (_isIndexed.HasValue && _isIndexed.Value)
            indexType = typeof(int);

        // Allow test overrides for member path.
        if (_memberPath is not null)
            memberPath = _memberPath;

        // Allow test overrides for expression.
        LambdaExpression? finalExpression = _expressionOverride ?? expression;

        // If composite members were added, build as composite.
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

    internal QueryField BuildComposite(
        string name,
        IReadOnlyList<LambdaExpression> members,
        Type valueType,
        Type? elementType) =>
        new(name, valueType, elementType, null, null, members, null, _converter,
            _filterable, _sortable, _searchable, _selectable, _groupable, _caseInsensitive, _allowNullSearch);
}
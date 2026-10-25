using System.Linq.Expressions;

namespace Core.Common.Querying.Mapping;

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

    internal QueryField Build(
        string name,
        LambdaExpression expression,
        string? memberPath,
        Type valueType,
        Type? elementType,
        Type? indexType) =>
        new(name, valueType, elementType, memberPath, expression, [], indexType, _converter,
            _filterable, _sortable, _searchable, _selectable, _groupable, _caseInsensitive, _allowNullSearch);

    internal QueryField BuildComposite(
        string name,
        IReadOnlyList<LambdaExpression> members,
        Type valueType,
        Type? elementType) =>
        new(name, valueType, elementType, null, null, members, null, _converter,
            _filterable, _sortable, _searchable, _selectable, _groupable, _caseInsensitive, _allowNullSearch);
}
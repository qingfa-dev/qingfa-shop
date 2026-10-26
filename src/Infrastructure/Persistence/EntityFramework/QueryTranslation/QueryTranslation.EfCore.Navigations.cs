using Application.Common.Querying;
using Application.Common.Querying.Mapping;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

public sealed partial class QueryTranslationEfCore<T> where T : class
{
    /// <summary>Eager-loads the navigations needed by a grouped or aggregated shape.</summary>
    public IQueryable<T> IncludeNavigations(
        IQueryable<T> source,
        Query<T> query,
        QueryMap<T> map)
    {
        var names = new List<string>();
        if (query.GroupBy is not null)
            names.AddRange(query.GroupBy.Select(key => key.Field));
        if (query.Aggregate is not null)
            names.AddRange(query.Aggregate.Select(spec => spec.Field));
        if (query.Select is not null)
            names.AddRange(query.Select.Select(select => select.Field));

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
        {
            // Validate: only mapped scalar members can contribute a navigation path.
            if (!map.TryGet(name, out QueryField? field) ||
                field.IsCollection ||
                field.IsIndexed ||
                field.Composite.Count > 0 ||
                field.Expression is null ||
                field.MemberPath is null)
            {
                continue;
            }

            // Translate: `Child.Name` -> Include("Child"); `Child.Parent.Name` ->
            // Include("Child.Parent"). The leaf is a scalar, not a navigation.
            int separator = field.MemberPath.LastIndexOf(QueryConstant.Tokens.Separator.Member);
            if (separator > 0)
                paths.Add(field.MemberPath[..separator]);
        }

        foreach (string path in paths)
            source = source.Include(path);

        return source;
    }
}
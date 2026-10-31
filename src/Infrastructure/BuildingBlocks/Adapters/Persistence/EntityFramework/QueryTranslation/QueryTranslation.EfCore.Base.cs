namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Translates the neutral query into EF Core expression trees.</summary>
/// <remarks>
/// The implementation is split across partial files by concern:
/// <list type="bullet">
///   <item><c>QueryTranslation.EfCore.Filter.cs</c> — filter tree → predicate.</item>
///   <item><c>QueryTranslation.EfCore.Search.cs</c> — free-text search → predicate.</item>
///   <item><c>QueryTranslation.EfCore.Sort.cs</c> — ordering and paging.</item>
///   <item><c>QueryTranslation.EfCore.Navigations.cs</c> — eager-load includes.</item>
///   <item><c>QueryTranslation.EfCore.Projection.cs</c> — server-side row projection.</item>
///   <item><c>QueryTranslation.EfCore.Aggregate.cs</c> — client-side grouping and aggregation.</item>
///   <item><c>QueryTranslation.EfCore.Expressions.cs</c> — shared expression-tree helpers.</item>
///   <item><c>QueryTranslation.EfCore.Fields.cs</c> — shared field resolution.</item>
/// </list>
/// </remarks>
public sealed partial class QueryTranslationEfCore<T> : IQueryTranslator<T> where T : class
{
    private readonly IQueryOperatorRegistry _operators;

    public QueryTranslationEfCore(IQueryOperatorRegistry? operators = null) =>
        _operators = operators ?? new EfOperatorRegistry();
}
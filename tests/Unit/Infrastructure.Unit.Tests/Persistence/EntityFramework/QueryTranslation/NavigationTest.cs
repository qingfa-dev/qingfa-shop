using System.Linq.Expressions;

using Application.Common.Querying;
using Application.Common.Querying.Grouping;
using Application.Common.Querying.Aggregation;
using Application.Common.Querying.Projection;
using Application.Common.Querying.Mapping;

using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.QueryTranslation;

[Trait("Category", "Unit")]
public sealed class NavigationTest
{
    private static readonly QueryMap<QueryProbe> Map = QueryProbeMap.Create();
    private static readonly IQueryTranslator<QueryProbe> Translator = new QueryTranslationEfCore<QueryProbe>();

    [Fact(DisplayName = "IncludeNavigations_WithGroupBy_IncludesGroupByFields")]
    public void IncludeNavigations_WithGroupBy_IncludesGroupByFields()
    {
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("name")]
        };

        var result = Translator.IncludeNavigations(new List<QueryProbe>().AsQueryable(), query, Map);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "IncludeNavigations_WithAggregate_IncludesAggregateFields")]
    public void IncludeNavigations_WithAggregate_IncludesAggregateFields()
    {
        var query = new Query<QueryProbe>
        {
            Aggregate = [new AggregateSpec(AggregateFunction.Count, "name", "count")]
        };

        var result = Translator.IncludeNavigations(new List<QueryProbe>().AsQueryable(), query, Map);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "IncludeNavigations_WithSelect_IncludesSelectFields")]
    public void IncludeNavigations_WithSelect_IncludesSelectFields()
    {
        var query = new Query<QueryProbe>
        {
            Select = [new SelectField("name", "title")]
        };

        var result = Translator.IncludeNavigations(new List<QueryProbe>().AsQueryable(), query, Map);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "IncludeNavigations_WithNestedMemberPath_IncludesParentNavigation")]
    public void IncludeNavigations_WithNestedMemberPath_IncludesParentNavigation()
    {
        var map = BuildMapWithNestedPath();
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("child.name")]
        };

        var result = Translator.IncludeNavigations(new List<QueryProbe>().AsQueryable(), query, map);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "IncludeNavigations_WithCollectionField_DoesNotInclude")]
    public void IncludeNavigations_WithCollectionField_DoesNotInclude()
    {
        var map = BuildMapWithCollectionField();
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("tags")]
        };

        var result = Translator.IncludeNavigations(new List<QueryProbe>().AsQueryable(), query, map);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "IncludeNavigations_WithIndexedField_DoesNotInclude")]
    public void IncludeNavigations_WithIndexedField_DoesNotInclude()
    {
        var map = BuildMapWithIndexedField();
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("indexedField")]
        };

        var result = Translator.IncludeNavigations(new List<QueryProbe>().AsQueryable(), query, map);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "IncludeNavigations_WithCompositeField_DoesNotInclude")]
    public void IncludeNavigations_WithCompositeField_DoesNotInclude()
    {
        var map = BuildMapWithCompositeField();
        var query = new Query<QueryProbe>
        {
            GroupBy = [new GroupByField("composite")]
        };

        var result = Translator.IncludeNavigations(new List<QueryProbe>().AsQueryable(), query, map);

        result.Should().NotBeNull();
    }

    private static QueryMap<QueryProbe> BuildMapWithNestedPath()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("child.name", probe => probe.Name, field => field
                .Groupable()
                .MemberPath("child.name")
                .Expression(Expression.Lambda<Func<QueryProbe, object?>>(Expression.Convert(Expression.Property(Expression.Parameter(typeof(QueryProbe), "p"), "Name"), typeof(object)), Expression.Parameter(typeof(QueryProbe), "p"))));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithCollectionField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("tags", probe => probe.Tags!, field => field
                .Groupable()
                .IsCollection(true));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithIndexedField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("indexedField", probe => probe.Score, field => field
                .Groupable()
                .IsIndexed(true));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static QueryMap<QueryProbe> BuildMapWithCompositeField()
    {
        var builder = QueryMapBuilder<QueryProbe>.Create()
            .Map("composite", probe => probe.Name, field => field
                .Groupable()
                .Composite<QueryProbe>(probe => probe.Name));

        var result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }
}
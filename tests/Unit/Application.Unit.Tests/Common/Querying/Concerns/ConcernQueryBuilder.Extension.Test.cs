using Application.Common.Querying;
using Application.Common.Querying.Concerns;
using Application.Common.Querying.Filtering;
using Application.Common.Querying.Mapping;

using Core.Common.Concerns.Content.Localizable;
using Core.Common.Concerns.Content.MediaAttachable;
using Core.Common.Concerns.Content.Metafieldable;
using Core.Common.Concerns.Content.Sluggable;
using Core.Common.Concerns.Content.Taggable;
using Core.Common.Concerns.Foundation.Identifiable;
using Core.Common.Concerns.Foundation.Referenceable;
using Core.Common.Concerns.Foundation.StoreScoped;
using Core.Common.Concerns.Foundation.Tenantable;
using Core.Common.Concerns.Foundation.Versionable;
using Core.Common.Concerns.Lifecycle.Activatable;
using Core.Common.Concerns.Lifecycle.Creatable;
using Core.Common.Concerns.Lifecycle.Modifiable;
using Core.Common.Concerns.Lifecycle.Publishable;
using Core.Common.Concerns.Lifecycle.SoftDeletable;
using Core.Common.Concerns.Organization.Categorizable;
using Core.Common.Concerns.Organization.Collectionable;
using Core.Common.Concerns.Organization.Hierarchical;
using Core.Common.Concerns.Organization.Positionable;

namespace Application.Unit.Tests.Common.Querying.Concerns;

/// <summary>Unit tests for the reusable concern query-builder extensions.</summary>
public sealed class ConcernQueryBuilderExtensionTest
{
    private static readonly Guid TenantId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StoreId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Instant = DateTimeOffset.UtcNow;

    [Fact(DisplayName = "ForTenant should add a scope condition")]
    public void ForTenant_ShouldAddScope()
    {
        FilterCondition condition = SingleCondition(
            QueryBuilder<Item>.Create(CreateMap()).ForTenant(TenantId));

        condition.Field.Should().Be("tenantId");
        condition.Value.Should().Be(TenantId);
    }

    [Fact(DisplayName = "ForTenant scope should not be escaped by a later OrWhere")]
    public void ForTenant_WithOrWhere_ShouldKeepScope()
    {
        Result<Query<Item>> result = QueryBuilder<Item>.Create(CreateMap())
            .ForTenant(TenantId)
            .Where(item => item.Reference == "a")
            .OrWhere(item => item.Reference == "b")
            .Build();

        result.IsSuccess.Should().BeTrue();
        var group = result.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.And);
        group.Children.OfType<FilterCondition>()
            .Should().Contain(condition => condition.Field == "tenantId");
        group.Children.OfType<FilterGroup>()
            .Should().ContainSingle()
            .Which.Logic.Should().Be(FilterLogic.Or);
    }

    [Fact(DisplayName = "IncludeDeleted should hide deleted rows by default")]
    public void IncludeDeleted_WhenFalse_ShouldAddDeletedScope()
    {
        FilterCondition condition = SingleCondition(
            QueryBuilder<Item>.Create(CreateMap()).IncludeDeleted(false));

        condition.Field.Should().Be("isDeleted");
        condition.Value.Should().Be(false);
    }

    [Fact(DisplayName = "IncludeDeleted should leave the filter untouched when true")]
    public void IncludeDeleted_WhenTrue_ShouldNotFilter()
    {
        Result<Query<Item>> result = QueryBuilder<Item>.Create(CreateMap())
            .IncludeDeleted()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Filter.Should().BeNull();
    }

    [Fact(DisplayName = "Soft-delete and creation filters should map to their concerns")]
    public void SoftDeleteAndCreation_ShouldAddConditions()
    {
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).OnlyDeleted())
            .Field.Should().Be("isDeleted");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).DeletedAfter(Instant))
            .Operator.Should().Be(FilterOperator.GreaterThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).DeletedBefore(Instant))
            .Operator.Should().Be(FilterOperator.LessThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).CreatedAfter(Instant))
            .Operator.Should().Be(FilterOperator.GreaterThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).CreatedBefore(Instant))
            .Operator.Should().Be(FilterOperator.LessThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).CreatedBy("alice"))
            .Value.Should().Be("alice");
    }

    [Fact(DisplayName = "Modification and activation filters should map to their concerns")]
    public void ModificationAndActivation_ShouldAddConditions()
    {
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ModifiedAfter(Instant))
            .Field.Should().Be("modifiedAtUtc");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ModifiedBefore(Instant))
            .Operator.Should().Be(FilterOperator.LessThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ModifiedBy("alice"))
            .Value.Should().Be("alice");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).OnlyActive())
            .Field.Should().Be("isActive");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ActivatedAfter(Instant))
            .Operator.Should().Be(FilterOperator.GreaterThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ActivatedBefore(Instant))
            .Operator.Should().Be(FilterOperator.LessThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ActivatedBy("alice"))
            .Field.Should().Be("activatedBy");
    }

    [Fact(DisplayName = "Publication and store filters should map to their concerns")]
    public void PublicationAndStore_ShouldAddConditions()
    {
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).OnlyPublished())
            .Field.Should().Be("isPublished");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).PublishedAfter(Instant))
            .Operator.Should().Be(FilterOperator.GreaterThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).PublishedBefore(Instant))
            .Operator.Should().Be(FilterOperator.LessThan);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).PublishedBy("alice"))
            .Value.Should().Be("alice");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ForStore(StoreId))
            .Field.Should().Be("storeId");
    }

    [Fact(DisplayName = "Reference, version, hierarchy, slug, and id filters should map")]
    public void ScalarConcerns_ShouldAddConditions()
    {
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithReference("R-1"))
            .Field.Should().Be("reference");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).ReferenceContains("R"))
            .Operator.Should().Be(FilterOperator.Contains);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).VersionAtLeast(2))
            .Operator.Should().Be(FilterOperator.GreaterThanOrEqual);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).VersionAtMost(4))
            .Operator.Should().Be(FilterOperator.LessThanOrEqual);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithParent(Guid.NewGuid()))
            .Field.Should().Be("parentId");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).RootOnly<Item, Guid>())
            .Value.Should().BeNull();
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithSlug("home"))
            .Field.Should().Be("slug");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).SlugContains("hom"))
            .Operator.Should().Be(FilterOperator.Contains);
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithId(Guid.NewGuid()))
            .Field.Should().Be("id");
    }

    [Fact(DisplayName = "Tag chains should add membership conditions")]
    public void TagChains_ShouldAddConditions()
    {
        FilterCondition tag = SingleCondition(
            QueryBuilder<Item>.Create(CreateMap()).WithTag("sale"));
        tag.Field.Should().Be("tags");
        tag.Operator.Should().Be(FilterOperator.Contains);

        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithoutTag("sale"))
            .Operator.Should().Be(FilterOperator.NotContains);

        Result<Query<Item>> any = QueryBuilder<Item>.Create(CreateMap())
            .WithAnyTag("a", "b")
            .Build();
        any.IsSuccess.Should().BeTrue();
        var group = any.Value.Filter.Should().BeOfType<FilterGroup>().Subject;
        group.Logic.Should().Be(FilterLogic.Or);
        group.Children.Should().HaveCount(2);

        Result<Query<Item>> all = QueryBuilder<Item>.Create(CreateMap())
            .WithAllTags("a", "b")
            .Build();
        all.IsSuccess.Should().BeTrue();
        Flatten(all.Value.Filter!).OfType<FilterCondition>().Should().HaveCount(2);

        Result<Query<Item>> emptyAny = QueryBuilder<Item>.Create(CreateMap())
            .WithAnyTag()
            .Build();
        emptyAny.IsSuccess.Should().BeTrue();
        emptyAny.Value.Filter.Should().BeNull();
    }

    [Fact(DisplayName = "Collection-membership chains should add conditions")]
    public void CollectionChains_ShouldAddConditions()
    {
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithCategory("cat"))
            .Field.Should().Be("categories");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithCollection("col"))
            .Field.Should().Be("collections");
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithMedia("media"))
            .Field.Should().Be("media");

        var metafield = new Metafield { Namespace = "ns", Key = "key" };
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithMetafield(metafield))
            .Field.Should().Be("metafields");

        var localized = new LocalizedValue { Property = "name", Locale = "en", Value = "Hello" };
        SingleCondition(QueryBuilder<Item>.Create(CreateMap()).WithLocalizedValue(localized))
            .Field.Should().Be("localizedValues");

        Result<Query<Item>> anyCategory = QueryBuilder<Item>.Create(CreateMap())
            .WithAnyCategory("a", "b")
            .Build();
        anyCategory.IsSuccess.Should().BeTrue();
        anyCategory.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);

        Result<Query<Item>> allCategory = QueryBuilder<Item>.Create(CreateMap())
            .WithAllCategories("a", "b")
            .Build();
        allCategory.IsSuccess.Should().BeTrue();
        Flatten(allCategory.Value.Filter!).OfType<FilterCondition>().Should().HaveCount(2);

        Result<Query<Item>> anyCollection = QueryBuilder<Item>.Create(CreateMap())
            .WithAnyCollection("a", "b")
            .Build();
        anyCollection.IsSuccess.Should().BeTrue();
        anyCollection.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);

        Result<Query<Item>> allCollection = QueryBuilder<Item>.Create(CreateMap())
            .WithAllCollections("a", "b")
            .Build();
        allCollection.IsSuccess.Should().BeTrue();
        Flatten(allCollection.Value.Filter!).OfType<FilterCondition>().Should().HaveCount(2);

        Result<Query<Item>> anyMedia = QueryBuilder<Item>.Create(CreateMap())
            .WithAnyMedia("a", "b")
            .Build();
        anyMedia.IsSuccess.Should().BeTrue();
        anyMedia.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);

        Result<Query<Item>> allMedia = QueryBuilder<Item>.Create(CreateMap())
            .WithAllMedia("a", "b")
            .Build();
        allMedia.IsSuccess.Should().BeTrue();
        Flatten(allMedia.Value.Filter!).OfType<FilterCondition>().Should().HaveCount(2);

        Result<Query<Item>> anyMetafield = QueryBuilder<Item>.Create(CreateMap())
            .WithAnyMetafield(metafield)
            .Build();
        anyMetafield.IsSuccess.Should().BeTrue();
        anyMetafield.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);

        Result<Query<Item>> allMetafields = QueryBuilder<Item>.Create(CreateMap())
            .WithAllMetafields(metafield)
            .Build();
        allMetafields.IsSuccess.Should().BeTrue();
        Flatten(allMetafields.Value.Filter!).OfType<FilterCondition>().Should().ContainSingle();

        Result<Query<Item>> anyLocalized = QueryBuilder<Item>.Create(CreateMap())
            .WithAnyLocalizedValue(localized)
            .Build();
        anyLocalized.IsSuccess.Should().BeTrue();
        anyLocalized.Value.Filter.Should().BeOfType<FilterGroup>()
            .Which.Logic.Should().Be(FilterLogic.Or);

        Result<Query<Item>> allLocalized = QueryBuilder<Item>.Create(CreateMap())
            .WithAllLocalizedValues(localized)
            .Build();
        allLocalized.IsSuccess.Should().BeTrue();
        Flatten(allLocalized.Value.Filter!).OfType<FilterCondition>().Should().ContainSingle();
    }

    [Fact(DisplayName = "Order chains should set the expected fields")]
    public void OrderChains_ShouldSetFields()
    {
        OrderField(QueryBuilder<Item>.Create(CreateMap()).OrderByCreated())
            .Should().Be("createdAtUtc");
        OrderField(QueryBuilder<Item>.Create(CreateMap()).OrderByModified())
            .Should().Be("modifiedAtUtc");
        OrderField(QueryBuilder<Item>.Create(CreateMap()).OrderByPosition())
            .Should().Be("position");
    }

    [Fact(DisplayName = "DefaultOrderByCreated should not override caller order")]
    public void DefaultOrderByCreated_WithCallerOrder_ShouldKeepIt()
    {
        Result<Query<Item>> result = QueryBuilder<Item>.Create(CreateMap())
            .OrderByPosition()
            .DefaultOrderByCreated()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().ContainSingle()
            .Which.Field.Should().Be("position");
    }

    [Fact(DisplayName = "DefaultOrderByCreated should add the default when empty")]
    public void DefaultOrderByCreated_WhenEmpty_ShouldAddDefault()
    {
        Result<Query<Item>> result = QueryBuilder<Item>.Create(CreateMap())
            .DefaultOrderByCreated()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderBy.Should().ContainSingle()
            .Which.Field.Should().Be("createdAtUtc");
    }

    private static FilterCondition SingleCondition(QueryBuilder<Item> builder)
    {
        Result<Query<Item>> result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value.Filter.Should().BeOfType<FilterCondition>().Subject;
    }

    private static IEnumerable<FilterNode> Flatten(FilterNode node) =>
        node is FilterGroup group
            ? group.Children.SelectMany(Flatten)
            : [node];

    private static string OrderField(QueryBuilder<Item> builder)
    {
        Result<Query<Item>> result = builder.Build();
        result.IsSuccess.Should().BeTrue();
        return result.Value.OrderBy.Should().ContainSingle().Subject.Field;
    }

    private static QueryMap<Item> CreateMap() =>
        QueryMapBuilder<Item>.Create()
            .Map("id", item => item.Id)
            .Map("tenantId", item => item.TenantId)
            .Map("storeId", item => item.StoreId)
            .Map("isDeleted", item => item.IsDeleted)
            .Map("deletedAtUtc", item => item.DeletedAtUtc)
            .Map("deletedBy", item => item.DeletedBy)
            .Map("createdAtUtc", item => item.CreatedAtUtc)
            .Map("createdBy", item => item.CreatedBy)
            .Map("modifiedAtUtc", item => item.ModifiedAtUtc)
            .Map("modifiedBy", item => item.ModifiedBy)
            .Map("isActive", item => item.IsActive)
            .Map("activatedAtUtc", item => item.ActivatedAtUtc)
            .Map("activatedBy", item => item.ActivatedBy)
            .Map("isPublished", item => item.IsPublished)
            .Map("publishedAtUtc", item => item.PublishedAtUtc)
            .Map("publishedBy", item => item.PublishedBy)
            .Map("reference", item => item.Reference)
            .Map("version", item => item.Version)
            .Map("position", item => item.Position)
            .Map("parentId", item => item.ParentId)
            .Map("slug", item => item.Slug)
            .Map("tags", item => item.Tags)
            .Map("categories", item => item.Categories)
            .Map("collections", item => item.Collections)
            .Map("media", item => item.Media)
            .Map("localizedValues", item => item.LocalizedValues)
            .Map("metafields", item => item.Metafields)
            .Build().Value;

    private sealed class Item :
        IIdentifiable<Guid>,
        ITenantable<Guid>,
        IStoreScoped<Guid>,
        ISoftDeletable,
        ICreatable,
        IModifiable,
        IActivatable,
        IPublishable,
        IReferenceable,
        IVersionable,
        IPositionable,
        IHierarchical<Guid>,
        ISluggable,
        ITaggable,
        ICategorizable<string>,
        ICollectionable<string>,
        IMediaAttachable<string>,
        ILocalizable,
        IMetafieldable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public Guid StoreId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset? ActivatedAtUtc { get; set; }
        public string? ActivatedBy { get; set; }
        public bool IsPublished { get; set; }
        public DateTimeOffset? PublishedAtUtc { get; set; }
        public string? PublishedBy { get; set; }
        public string Reference { get; set; } = string.Empty;
        public long Version { get; set; }
        public int Position { get; set; }
        public Guid? ParentId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public ICollection<string> Tags { get; } = [];
        public ICollection<string> Categories { get; } = [];
        public ICollection<string> Collections { get; } = [];
        public ICollection<string> Media { get; } = [];
        public ICollection<LocalizedValue> LocalizedValues { get; } = [];
        public ICollection<Metafield> Metafields { get; } = [];
    }
}

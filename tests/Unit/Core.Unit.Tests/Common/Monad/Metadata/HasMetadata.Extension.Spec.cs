namespace Core.Unit.Tests.Common.Monad.Metadata;

/// <summary>
/// Tests for <see cref="HasMetadataExtensions"/> metadata access,
/// well-known keys, mutation, conversion, and argument guards.
/// </summary>
[Trait("Category", "Unit")]
public class HasMetadataExtensionSpec
{
    private static readonly DateTimeOffset FixedTimestamp =
    new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

    private static readonly Guid FixedCorrelationId =
        Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

    private static readonly Guid FixedRequestId =
        Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");

    private static readonly Guid FixedEventId =
        Guid.Parse("cccccccc-1111-2222-3333-444444444444");

    private static readonly Guid FixedCausationId =
        Guid.Parse("dddddddd-1111-2222-3333-444444444444");

    private static readonly Guid FixedTenantId =
        Guid.Parse("eeeeeeee-1111-2222-3333-444444444444");

    private static HasMetadataStub<DefaultMetadata> StubWith(
        string key,
        object value) =>
        new()
        {
            Metadata = DefaultMetadata.Create().With(key, value)
        };

    private static HasMetadataStub<DefaultMetadata> EmptyStub() =>
        new()
        {
            Metadata = DefaultMetadata.Create()
        };

    public static TheoryData<string, Guid> GuidMetadataCases => new()
{
    {
        MetadataConstant.Keyword.CorrelationId,
        Guid.Parse("11111111-2222-3333-4444-555555555555")
    },
    {
        MetadataConstant.Keyword.RequestId,
        Guid.Parse("22222222-3333-4444-5555-666666666666")
    },
    {
        MetadataConstant.Keyword.CausationId,
        Guid.Parse("33333333-4444-5555-6666-777777777777")
    }
};

    #region GetValueOrDefault

    [Theory]
    [InlineData(42)]
    [InlineData("value")]
    public void GetValueOrDefault_WhenKeyIsPresent_ShouldReturnRawValue(
        object value)
    {
        IHasMetadata<DefaultMetadata> source = StubWith("k", value);

        source.GetValueOrDefault("k").Should().Be(value);
    }

    [Fact]
    public void GetValueOrDefault_WhenKeyIsMissing_ShouldReturnNull()
    {
        IHasMetadata<DefaultMetadata> source = EmptyStub();

        source.GetValueOrDefault("missing").Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefault_WhenValueIsConvertible_ShouldReturnTypedValue()
    {
        IHasMetadata<DefaultMetadata> source = StubWith("k", "42");

        source.GetValueOrDefault<DefaultMetadata, int>("k")
            .Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_WhenKeyIsMissing_ShouldReturnFallback()
    {
        IHasMetadata<DefaultMetadata> source = EmptyStub();

        source.GetValueOrDefault("version", 7).Should().Be(7);
    }

    [Fact]
    public void GetValueOrDefault_WhenSourceIsNull_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = null!;

        AssertSourceNullThrows(() =>
        {
            _ = source.GetValueOrDefault("k");
        });
    }

    #endregion

    #region GetRequiredValue

    [Fact]
    public void GetRequiredValue_WhenKeyIsPresent_ShouldReturnRawValue()
    {
        IHasMetadata<DefaultMetadata> source = StubWith("k", 42);

        source.GetRequiredValue("k").Should().Be(42);
    }

    [Fact]
    public void GetRequiredValue_WhenKeyIsMissing_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = EmptyStub();

        Action act = () => { _ = source.GetRequiredValue("missing"); };

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void GetRequiredValue_WhenValueIsConvertible_ShouldReturnTypedValue()
    {
        IHasMetadata<DefaultMetadata> source = StubWith("k", "42");

        source.GetRequiredValue<DefaultMetadata, int>("k")
            .Should().Be(42);
    }

    [Fact]
    public void GetRequiredValue_WhenValueIsNotConvertible_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = StubWith("k", "abc");

        Action act = () =>
        {
            _ = source.GetRequiredValue<DefaultMetadata, int>("k");
        };

        act.Should().Throw<Exception>();
    }

    #endregion

    #region TryGetValue

    [Fact]
    public void TryGetValue_WhenValueIsConvertible_ShouldReturnValue()
    {
        IHasMetadata<DefaultMetadata> source = StubWith("k", "42");

        var found = source.TryGetValue("k", out int value);

        found.Should().BeTrue();
        value.Should().Be(42);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryGetValue_WhenValueCannotBeReadAsInt_ShouldReturnFalse(
        bool missing)
    {
        IHasMetadata<DefaultMetadata> source = missing
            ? EmptyStub()
            : StubWith("k", "abc");

        var found = source.TryGetValue("k", out int value);

        found.Should().BeFalse();
        value.Should().Be(default);
    }

    [Fact]
    public void TryGetValue_WhenSourceIsNull_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = null!;

        AssertSourceNullThrows(() =>
        {
            _ = source.TryGetValue("k", out int _);
        });
    }

    #endregion

    #region Contains

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Contains_ShouldReflectKeyPresence(bool keyExists)
    {
        IHasMetadata<DefaultMetadata> source = keyExists
            ? StubWith("k", 42)
            : EmptyStub();

        source.Contains("k").Should().Be(keyExists);
    }

    [Fact]
    public void Contains_WhenSourceIsNull_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = null!;

        AssertSourceNullThrows(() =>
        {
            _ = source.Contains("k");
        });
    }

    #endregion

    #region SetValue

    [Fact]
    public void SetValue_ShouldAddOrOverwriteValue()
    {
        var source = EmptyStub();

        source.SetValue("k", 1);
        source.Metadata["k"].Should().Be(1);

        source.SetValue("k", 2);
        source.Metadata["k"].Should().Be(2);
    }

    [Fact]
    public void SetValue_WhenSourceIsNull_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = null!;

        AssertSourceNullThrows(() =>
        {
            source.SetValue("k", 42);
        });
    }

    #endregion

    #region Remove

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Remove_ShouldReflectKeyPresence(bool keyExists)
    {
        var source = keyExists
            ? StubWith("k", 42)
            : EmptyStub();

        var removed = source.Remove("k");

        removed.Should().Be(keyExists);
        source.Metadata.ContainsKey("k").Should().BeFalse();
    }

    [Fact]
    public void Remove_WhenSourceIsNull_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = null!;

        AssertSourceNullThrows(() =>
        {
            _ = source.Remove("k");
        });
    }

    #endregion

    #region MergeMetadata

    [Fact]
    public void MergeMetadata_ShouldOverwriteAndAddEntries()
    {
        // Arrange
        var source = StubWith(
            MetadataConstant.Keyword.CorrelationId,
            FixedCorrelationId);
        IMetadata metadata = DefaultMetadata.Create()
            .With(MetadataConstant.Keyword.CorrelationId, FixedRequestId)
            .With(MetadataConstant.Keyword.TenantId, FixedTenantId);

        // Act
        var merged = source.MergeMetadata(metadata);

        // Assert
        merged.Should().BeSameAs(source.Metadata);
        source.GetCorrelationId().Should().Be(FixedRequestId);
        source.GetTenantId().Should().Be(FixedTenantId);
    }

    [Fact]
    public void MergeMetadata_WhenSourceIsNull_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = null!;

        AssertSourceNullThrows(() =>
        {
            _ = source.MergeMetadata(DefaultMetadata.Create());
        });
    }

    [Fact]
    public void MergeMetadata_WhenMetadataIsNull_ShouldThrow()
    {
        // Arrange
        var source = EmptyStub();
        IMetadata metadata = null!;

        // Act
        Action act = () => _ = source.MergeMetadata(metadata);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("metadata");
    }

    #endregion

    #region Well-known metadata

    [Theory]
    [MemberData(nameof(GuidMetadataCases))]
    public void GuidMetadataHelpers_ShouldReturnParsedValue(
        string key,
        Guid expected)
    {
        IHasMetadata<DefaultMetadata> source =
            StubWith(key, expected.ToString());

        var actual = key switch
        {
            var k when k == MetadataConstant.Keyword.CorrelationId =>
                source.GetCorrelationId(),
            var k when k == MetadataConstant.Keyword.RequestId =>
                source.GetRequestId(),
            var k when k == MetadataConstant.Keyword.CausationId =>
                source.GetCausationId(),
            _ => throw new ArgumentOutOfRangeException(nameof(key))
        };

        actual.Should().Be(expected);
    }

    [Fact]
    public void GetTimestamp_ShouldReturnMetadataValue()
    {
        IHasMetadata<DefaultMetadata> source =
            StubWith(MetadataConstant.Keyword.Timestamp, FixedTimestamp);

        source.GetTimestamp().Should().Be(FixedTimestamp);
    }

    [Fact]
    public void GetEventId_ShouldReturnMetadataValue()
    {
        IHasMetadata<DefaultMetadata> source =
            StubWith(MetadataConstant.Keyword.EventId, FixedEventId);

        source.GetEventId().Should().Be(FixedEventId);
    }

    [Fact]
    public void GetEventId_WhenOnlyRequestIdIsPresent_ShouldReturnNull()
    {
        IHasMetadata<DefaultMetadata> source =
            StubWith(MetadataConstant.Keyword.RequestId, FixedEventId);

        source.GetEventId().Should().BeNull();
    }

    [Fact]
    public void GetTenantId_ShouldReturnMetadataValue()
    {
        IHasMetadata<DefaultMetadata> source =
            StubWith(MetadataConstant.Keyword.TenantId, FixedTenantId);

        source.GetTenantId().Should().Be(FixedTenantId);
    }

    [Fact]
    public void SetCorrelationId_ShouldBeReadableViaGetCorrelationId()
    {
        var source = EmptyStub();

        source.SetCorrelationId(FixedCorrelationId);

        source.GetCorrelationId().Should().Be(FixedCorrelationId);
    }

    [Fact]
    public void SetRequestId_ShouldBeReadableViaGetRequestId()
    {
        var source = EmptyStub();

        source.SetRequestId(FixedRequestId);

        source.GetRequestId().Should().Be(FixedRequestId);
    }

    [Fact]
    public void SetCausationId_ShouldBeReadableViaGetCausationId()
    {
        var source = EmptyStub();

        source.SetCausationId(FixedCausationId);

        source.GetCausationId().Should().Be(FixedCausationId);
    }

    [Fact]
    public void SetTimestamp_ShouldBeReadableViaGetTimestamp()
    {
        var source = EmptyStub();

        source.SetTimestamp(FixedTimestamp);

        source.GetTimestamp().Should().Be(FixedTimestamp);
    }

    [Fact]
    public void SetTenantId_ShouldBeReadableViaGetTenantId()
    {
        var source = EmptyStub();

        source.SetTenantId(FixedTenantId);

        source.GetTenantId().Should().Be(FixedTenantId);
    }

    [Fact]
    public void SetEventId_ShouldStoreUnderEventIdKeywordNotRequestId()
    {
        var source = EmptyStub();

        source.SetEventId(FixedEventId);

        source.Contains(MetadataConstant.Keyword.EventId).Should().BeTrue();
        source.Contains(MetadataConstant.Keyword.RequestId).Should().BeFalse();
    }

    [Fact]
    public void SetWellKnownValue_WhenSourceIsNull_ShouldThrow()
    {
        IHasMetadata<DefaultMetadata> source = null!;

        var setCorrelation = () => source.SetCorrelationId(FixedCorrelationId);
        var setTenant = () => source.SetTenantId(FixedTenantId);

        setCorrelation.Should().Throw<ArgumentNullException>();
        setTenant.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Contract

    [Fact]
    public void Extensions_ShouldAcceptGenericMetadataContract()
    {
        IHasMetadata<DefaultMetadata> source = EmptyStub();

        source.GetValueOrDefault("missing").Should().BeNull();
    }

    #endregion

    #region Assertion helpers

    private static void AssertSourceNullThrows(Action act)
    {
        var exception = act.Should()
            .Throw<ArgumentNullException>()
            .Which;

        exception.ParamName.Should().Be("source");
    }

    #endregion

}
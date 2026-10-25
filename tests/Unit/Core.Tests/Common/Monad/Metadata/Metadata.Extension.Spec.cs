namespace Core.UnitTests.Common.Monad.Metadata;

/// <summary>
/// Tests for <see cref="MetadataExtensions"/> typed access, conversion,
/// presence, and mutation.
/// </summary>
/// <remarks>
/// Covers raw access, typed conversion, required access, try-get semantics,
/// presence checks, mutation behavior, well-defined argument guards,
/// and conversion edge cases.
/// </remarks>
[Trait("Category", "Unit")]
public class MetadataExtensionSpec
{
    /// <summary>
    /// Stub enum used by typed conversion tests.
    /// </summary>
    public enum StubMetadataKind
    {
        None = 0,
        Alpha = 1,
        Beta = 2,
    }

    #region Helpers

    /// <summary>
    /// Creates metadata containing one key/value pair.
    /// </summary>
    private static MetadataStub StubWith(
        string key,
        object value)
        => new MetadataStub()
            .With(key, value);

    /// <summary>
    /// Reads a metadata value as <typeparamref name="TValue"/>.
    /// </summary>
    private static TValue? Get<TValue>(
        IMetadata metadata,
        string key)
        => metadata.GetValueOrDefault<TValue>(key);

    /// <summary>
    /// Reads a required metadata value as <typeparamref name="TValue"/>.
    /// </summary>
    private static TValue GetRequired<TValue>(
        IMetadata metadata,
        string key)
        => metadata.GetRequiredValue<TValue>(key);

    #endregion

    #region Raw access

    [Fact]
    public void GetValueOrDefault_WhenKeyPresent_ShouldReturnRawValue()
    {
        // Arrange
        IMetadata metadata = StubWith("k", 42);

        // Act
        var actual = metadata.GetValueOrDefault("k");

        // Assert
        actual.Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_WhenKeyMissing_ShouldReturnNull()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        var actual = metadata.GetValueOrDefault("k");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefault_WhenMetadataNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = null!;

        // Act
        Action act = () => metadata.GetValueOrDefault("k");

        // Assert
        var exception = act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("metadata");
    }

    [Fact]
    public void GetValueOrDefault_WhenKeyNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act = () => metadata.GetValueOrDefault(null!);

        // Assert
        var exception = act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("metadataKey");
        exception.Which.Message.Should().Contain(
            MetadataOutcome.Failure.Key.Argument.Null.Representation);
    }

    #endregion

    #region Typed access

    [Theory]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    public void GetValueOrDefault_WhenStringRepresentsInt_ShouldReturnConverted(
        string stored,
        int expected)
    {
        // Arrange
        IMetadata metadata = StubWith("k", stored);

        // Act
        var actual = Get<int>(metadata, "k");

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetValueOrDefault_WhenLongRepresentsInt_ShouldReturnConverted()
    {
        // Arrange
        IMetadata metadata = StubWith("k", 42L);

        // Act
        var actual = Get<int>(metadata, "k");

        // Assert
        actual.Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_WhenValueIsIncompatible_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "abc");

        // Act
        var actual = Get<int>(metadata, "k");

        // Assert
        actual.Should().Be(0);
    }

    [Fact]
    public void GetValueOrDefault_WhenIntConvertedToString_ShouldReturnInvariantValue()
    {
        // Arrange
        IMetadata metadata = StubWith("k", 42);

        // Act
        var actual = Get<string>(metadata, "k");

        // Assert
        actual.Should().Be("42");
    }

    [Fact]
    public void GetValueOrDefault_WhenKeyMissingForReferenceTarget_ShouldReturnNull()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        var actual = Get<string>(metadata, "k");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefault_WhenKeyMissingForNullableTarget_ShouldReturnNull()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        var actual = Get<int?>(metadata, "k");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefault_WhenStringRepresentsNullableInt_ShouldReturnConverted()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "5");

        // Act
        var actual = Get<int?>(metadata, "k");

        // Assert
        actual.Should().Be(5);
    }

    [Theory]
    [InlineData(false, null, 7)]
    [InlineData(true, "abc", 7)]
    [InlineData(true, "42", 42)]
    public void GetValueOrDefaultWithFallback_ShouldReturnExpectedValue(
        bool keyExists,
        string? storedValue,
        int expected)
    {
        // Arrange
        IMetadata metadata = keyExists
            ? StubWith("version", storedValue!)
            : new MetadataStub();

        // Act
        var actual = metadata.GetValueOrDefault("version", 7);

        // Assert
        actual.Should().Be(expected);
    }

    #endregion

    #region Guid conversion

    [Fact]
    public void GetValueOrDefaultGuidFromString_ShouldParse()
    {
        // Arrange
        var expected =
            Guid.Parse("11111111-2222-3333-4444-555555555555");

        IMetadata metadata = StubWith("k", expected.ToString());

        // Act
        var actual = Get<Guid>(metadata, "k");

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetValueOrDefaultGuidFromGuid_ShouldReturnSameValue()
    {
        // Arrange
        var expected =
            Guid.Parse("11111111-2222-3333-4444-555555555555");

        IMetadata metadata = StubWith("k", expected);

        // Act
        var actual = Get<Guid>(metadata, "k");

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetValueOrDefaultGuidFromInvalidString_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "not-a-guid");

        // Act
        var actual = Get<Guid>(metadata, "k");

        // Assert
        actual.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GetValueOrDefaultGuidFromNumericValue_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", 42);

        // Act
        var actual = Get<Guid>(metadata, "k");

        // Assert
        actual.Should().Be(Guid.Empty);
    }

    #endregion

    #region DateTime conversion

    [Fact]
    public void GetValueOrDefaultDateTimeFromString_ShouldParse()
    {
        // Arrange
        IMetadata metadata =
            StubWith("k", "2024-06-15T10:30:00Z");

        // Act
        var actual = Get<DateTime>(metadata, "k");

        // Assert
        actual.Should().Be(
            new DateTime(
                2024,
                6,
                15,
                10,
                30,
                0,
                DateTimeKind.Utc));
    }

    [Fact]
    public void GetValueOrDefaultDateTimeFromDateTimeOffset_ShouldReturnUtc()
    {
        // Arrange
        var source =
            new DateTimeOffset(
                2024,
                6,
                15,
                12,
                30,
                0,
                TimeSpan.FromHours(2));

        IMetadata metadata = StubWith("k", source);

        // Act
        var actual = Get<DateTime>(metadata, "k");

        // Assert
        actual.Should().Be(source.UtcDateTime);
    }

    [Fact]
    public void GetValueOrDefaultDateTime_WhenStringIsInvalid_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "not-a-date");

        // Act
        var actual = Get<DateTime>(metadata, "k");

        // Assert
        actual.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region DateTimeOffset conversion

    [Fact]
    public void GetValueOrDefaultDateTimeOffsetFromString_ShouldParse()
    {
        // Arrange
        var expected =
            new DateTimeOffset(
                2024,
                6,
                15,
                10,
                30,
                0,
                TimeSpan.FromHours(2));

        IMetadata metadata =
            StubWith("k", "2024-06-15T10:30:00+02:00");

        // Act
        var actual = Get<DateTimeOffset>(metadata, "k");

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetValueOrDefaultDateTimeOffsetFromDateTime_ShouldUseZeroOffset()
    {
        // Arrange
        var source =
            new DateTime(
                2024,
                6,
                15,
                10,
                30,
                0,
                DateTimeKind.Utc);

        IMetadata metadata = StubWith("k", source);

        // Act
        var actual = Get<DateTimeOffset>(metadata, "k");

        // Assert
        actual.Should().Be(new DateTimeOffset(source, TimeSpan.Zero));
    }

    [Fact]
    public void GetValueOrDefaultDateTimeOffset_WhenStringIsInvalid_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "not-a-date");

        // Act
        var actual = Get<DateTimeOffset>(metadata, "k");

        // Assert
        actual.Should().Be(DateTimeOffset.MinValue);
    }

    #endregion

    #region Enum conversion

    [Theory]
    [InlineData("alpha", StubMetadataKind.Alpha)]
    [InlineData("BETA", StubMetadataKind.Beta)]
    [InlineData("2", StubMetadataKind.Beta)]
    public void GetValueOrDefaultEnum_ShouldParseNameOrNumericValue(
        string stored,
        StubMetadataKind expected)
    {
        // Arrange
        IMetadata metadata = StubWith("k", stored);

        // Act
        var actual = Get<StubMetadataKind>(metadata, "k");

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetValueOrDefaultEnumFromNumericValue_ShouldReturnConverted()
    {
        // Arrange
        IMetadata metadata = StubWith("k", 2);

        // Act
        var actual = Get<StubMetadataKind>(metadata, "k");

        // Assert
        actual.Should().Be(StubMetadataKind.Beta);
    }

    [Fact]
    public void GetValueOrDefaultEnum_WhenNameIsInvalid_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "Nope");

        // Act
        var actual = Get<StubMetadataKind>(metadata, "k");

        // Assert
        actual.Should().Be(StubMetadataKind.None);
    }

    [Fact]
    public void GetValueOrDefaultEnum_WhenValueIsIncompatible_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", DateTime.UnixEpoch);

        // Act
        var actual = Get<StubMetadataKind>(metadata, "k");

        // Assert
        actual.Should().Be(StubMetadataKind.None);
    }

    #endregion

    #region TypeConverter conversion

    [Fact]
    public void GetValueOrDefaultTimeSpanFromString_ShouldConvert()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "01:30:00");

        // Act
        var actual = Get<TimeSpan>(metadata, "k");

        // Assert
        actual.Should().Be(TimeSpan.FromMinutes(90));
    }

    [Fact]
    public void GetValueOrDefaultVersionFromEmptyString_ShouldReturnNull()
    {
        // Arrange
        IMetadata metadata = StubWith("k", string.Empty);

        // Act
        var actual = Get<Version>(metadata, "k");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefaultUriFromEmptyString_ShouldReturnNull()
    {
        // Arrange
        IMetadata metadata = StubWith("k", string.Empty);

        // Act
        var actual = Get<Uri>(metadata, "k");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefaultUriFromString_ShouldParse()
    {
        // Arrange
        var expected =
            new Uri("https://example.com/orders/42");

        IMetadata metadata =
            StubWith("k", expected.ToString());

        // Act
        var actual = Get<Uri>(metadata, "k");

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetValueOrDefault_WhenTargetIsOpaque_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "x");

        // Act
        var actual = Get<StubOpaque>(metadata, "k");

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GetValueOrDefault_WhenSourceIsOpaqueAndTargetIsInt_ShouldReturnDefault()
    {
        // Arrange
        IMetadata metadata =
            StubWith("k", new StubOpaque());

        // Act
        var actual = Get<int>(metadata, "k");

        // Assert
        actual.Should().Be(0);
    }

    #endregion

    #region Required access

    [Fact]
    public void GetRequiredValue_WhenKeyPresent_ShouldReturnRawValue()
    {
        // Arrange
        IMetadata metadata = StubWith("k", 42);

        // Act
        var actual = metadata.GetRequiredValue("k");

        // Assert
        actual.Should().Be(42);
    }

    [Fact]
    public void GetRequiredValue_WhenKeyMissing_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act = () => metadata.GetRequiredValue("k");

        // Assert
        var exception =
            act.Should().Throw<KeyNotFoundException>();

        exception.Which.Message.Should().Contain(
            MetadataOutcome.Failure.Key.Metadata.NotFound.Representation);
    }

    [Fact]
    public void GetRequiredValue_WhenStoredValueIsNull_ShouldThrow()
    {
        // Arrange
        var dictionary =
            new Dictionary<string, object>
            {
                ["k"] = null!
            };

        IMetadata metadata =
            new DefaultMetadata(dictionary);

        // Act
        Action act = () => metadata.GetRequiredValue("k");

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void GetRequiredValue_WhenValueIsConvertible_ShouldReturnConverted()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "42");

        // Act
        var actual = GetRequired<int>(metadata, "k");

        // Assert
        actual.Should().Be(42);
    }

    [Fact]
    public void GetRequiredValue_WhenKeyIsMissingForTypedTarget_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act = () => GetRequired<int>(metadata, "k");

        // Assert
        var exception =
            act.Should().Throw<KeyNotFoundException>();

        exception.Which.Message.Should().Contain(
            MetadataOutcome.Failure.Key.Metadata.NotFound.Representation);
    }

    [Fact]
    public void GetRequiredValue_WhenValueIsNotConvertible_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "abc");

        // Act
        Action act = () => GetRequired<int>(metadata, "k");

        // Assert
        var exception =
            act.Should().Throw<InvalidCastException>();

        exception.Which.Message.Should().Contain(
            MetadataOutcome.Failure.Value.Metadata.InvalidType.Representation);
    }

    [Fact]
    public void GetRequiredValue_WhenConversionReturnsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata =
            StubWith("k", new StubOpaque());

        // Act
        Action act = () => GetRequired<string>(metadata, "k");

        // Assert
        act.Should().Throw<InvalidCastException>();
    }

    #endregion

    #region TryGetValue

    [Fact]
    public void TryGetValue_WhenPresentAndConvertible_ShouldReturnTrue()
    {
        // Arrange
        IMetadata metadata = StubWith("k", 42);

        // Act
        var found =
            metadata.TryGetValue("k", out int value);

        // Assert
        found.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_WhenPresentButIncompatible_ShouldReturnFalse()
    {
        // Arrange
        IMetadata metadata = StubWith("k", "abc");

        // Act
        var found =
            metadata.TryGetValue("k", out int value);

        // Assert
        found.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_WhenKeyIsMissingForValueTarget_ShouldReturnFalse()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        var found =
            metadata.TryGetValue("k", out int value);

        // Assert
        found.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_WhenKeyIsMissingForReferenceTarget_ShouldReturnFalse()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        var found =
            metadata.TryGetValue("k", out string? value);

        // Assert
        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_WhenMetadataIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = null!;

        // Act
        Action act =
            () => metadata.TryGetValue("k", out int _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetValue_WhenKeyIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act =
            () => metadata.TryGetValue(null!, out int _);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("metadataKey");
    }

    #endregion

    #region Presence

    [Fact]
    public void Contains_WhenKeyIsPresentDifferentCase_ShouldReturnTrue()
    {
        // Arrange
        IMetadata metadata =
            StubWith(
                MetadataConstant.Keyword.CorrelationId,
                "abc");

        // Act
        var actual =
            metadata.Contains("correlationid");

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void Contains_WhenKeyIsMissing_ShouldReturnFalse()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        var actual = metadata.Contains("k");

        // Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void Contains_WhenMetadataIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = null!;

        // Act
        Action act = () => metadata.Contains("k");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Contains_WhenKeyIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act = () => metadata.Contains(null!);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("metadataKey");
    }

    #endregion

    #region Mutation

    [Fact]
    public void SetValue_ShouldAddEntry()
    {
        // Arrange
        IMetadata metadata = DefaultMetadata.Create();

        // Act
        metadata.SetValue("k", 1);

        // Assert
        metadata["k"].Should().Be(1);
    }

    [Fact]
    public void SetValue_ShouldOverwriteEntryCaseInsensitively()
    {
        // Arrange
        IMetadata metadata =
            DefaultMetadata.Create().With("k", 1);

        // Act
        metadata.SetValue("K", 2);

        // Assert
        metadata.Count.Should().Be(1);
        metadata["k"].Should().Be(2);
    }

    [Fact]
    public void SetValue_WhenMetadataIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = null!;

        // Act
        Action act =
            () => metadata.SetValue("k", 1);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetValue_WhenKeyIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act =
            () => metadata.SetValue(null!, 1);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("metadataKey");
    }

    [Fact]
    public void SetValue_WhenValueIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act =
            () => metadata.SetValue("k", null!);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("value");
    }

    [Fact]
    public void SetValue_WhenMetadataIsReadOnly_ShouldThrowNotSupported()
    {
        // Arrange
        IMetadata metadata =
            new MetadataStub();

        // Act
        Action act =
            () => metadata.SetValue("k", 1);

        // Assert
        var exception =
            act.Should().Throw<NotSupportedException>();

        exception.Which.Message.Should().Contain(
            MetadataOutcome.Failure.Dictionary.Mutation.NotSupported.Representation);
    }

    [Fact]
    public void MergeMetadata_ShouldCopySourceEntriesIntoDestination()
    {
        // Arrange
        DefaultMetadata destination =
            DefaultMetadata.Create().With("a", 1);
        IMetadata source =
            DefaultMetadata.Create().With("b", 2);

        // Act
        var merged = destination.MergeMetadata(source);

        // Assert
        merged.Should().BeSameAs(destination);
        merged["a"].Should().Be(1);
        merged["b"].Should().Be(2);
    }

    [Fact]
    public void MergeMetadata_ShouldOverwriteDestinationEntriesFromSource()
    {
        // Arrange
        DefaultMetadata destination =
            DefaultMetadata.Create()
                .With("k", 1)
                .With("only-destination", true);
        IMetadata source =
            DefaultMetadata.Create().With("K", 2);

        // Act
        var merged = destination.MergeMetadata(source);

        // Assert
        merged.Count.Should().Be(2);
        merged["k"].Should().Be(2);
        merged["only-destination"].Should().Be(true);
    }

    [Fact]
    public void MergeMetadata_WhenDestinationIsNull_ShouldThrow()
    {
        // Arrange
        DefaultMetadata destination = null!;
        IMetadata source = new MetadataStub();

        // Act
        Action act =
            () => destination.MergeMetadata(source);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("destination");
    }

    [Fact]
    public void MergeMetadata_WhenSourceIsNull_ShouldThrow()
    {
        // Arrange
        DefaultMetadata destination = DefaultMetadata.Create();
        IMetadata source = null!;

        // Act
        Action act =
            () => destination.MergeMetadata(source);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("source");
    }

    [Fact]
    public void MergeMetadata_WhenDestinationIsReadOnly_ShouldThrowNotSupported()
    {
        // Arrange
        IMetadata destination = new MetadataStub();
        IMetadata source =
            DefaultMetadata.Create().With("k", 1);

        // Act
        Action act =
            () => destination.MergeMetadata(source);

        // Assert
        var exception =
            act.Should().Throw<NotSupportedException>();

        exception.Which.Message.Should().Contain(
            MetadataOutcome.Failure.Dictionary.Mutation.NotSupported.Representation);
    }

    [Fact]
    public void Remove_WhenKeyIsPresent_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        IMetadata metadata =
            DefaultMetadata.Create().With("k", 1);

        // Act
        var removed =
            metadata.Remove("k");

        // Assert
        removed.Should().BeTrue();
        metadata.ContainsKey("k").Should().BeFalse();
    }

    [Fact]
    public void Remove_WhenKeyIsMissing_ShouldReturnFalse()
    {
        // Arrange
        IMetadata metadata =
            new MetadataStub();

        // Act
        var removed =
            metadata.Remove("k");

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public void Remove_WhenMetadataIsReadOnly_ShouldReturnFalse()
    {
        // Arrange
        IMetadata metadata =
            new MetadataStub();

        // Act
        var removed =
            metadata.Remove("k");

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public void Remove_WhenMetadataIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = null!;

        // Act
        Action act =
            () => metadata.Remove("k");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Remove_WhenKeyIsNull_ShouldThrow()
    {
        // Arrange
        IMetadata metadata = new MetadataStub();

        // Act
        Action act =
            () => metadata.Remove(null!);

        // Assert
        var exception =
            act.Should().Throw<ArgumentNullException>();

        exception.Which.ParamName.Should().Be("metadataKey");
    }

    #endregion

    #region Contract

    [Fact]
    public void Metadata_ShouldExposeReadOnlyDictionaryContract()
    {
        // Arrange
        IMetadata metadata =
            new MetadataStub();

        // Act

        // Assert
        metadata.Should().BeAssignableTo<IReadOnlyDictionary<string, object>>();
    }

    #endregion


}
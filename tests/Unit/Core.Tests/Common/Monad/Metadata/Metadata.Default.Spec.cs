namespace Core.Tests.Common.Monad.Metadata;

/// <summary>Tests for <see cref="DefaultMetadata"/> construction, factories, and mutation.</summary>
[Trait("Category", "Unit")]
public class DefaultMetadataSpec
{
    [Fact]
    public void New_ShouldCreateEmptyCaseInsensitiveStore()
    {
        var dictionary = new DefaultMetadata();

        dictionary["Alpha"] = 1;

        dictionary.Count.Should().Be(1);
        dictionary.ContainsKey("ALPHA").Should().BeTrue();
        dictionary["alpha"].Should().Be(1);
    }

    [Fact]
    public void NewFromDictionary_ShouldCopyEntriesAndNotShareSource()
    {
        var source = new Dictionary<string, object>
        {
            ["CorrelationId"] = "abc"
        };

        var dictionary = new DefaultMetadata(source);
        source["CorrelationId"] = "changed";

        dictionary.Count.Should().Be(1);
        dictionary["correlationid"].Should().Be("abc");
    }

    [Fact]
    public void NewFromDictionary_WhenNull_ShouldThrow()
    {
        IDictionary<string, object> source = null!;

        AssertArgumentNull(
            () => new DefaultMetadata(source),
            "dictionary",
            MetadataOutcome.Failure.Dictionary.Argument.Null.Representation);
    }

    [Fact]
    public void NewFromPairs_ShouldCreateStore()
    {
        var pairs = new[]
        {
        new KeyValuePair<string, object>("CorrelationId", "abc"),
        new KeyValuePair<string, object>("RequestId", "def")
    };

        var dictionary = new DefaultMetadata(pairs);

        dictionary.Count.Should().Be(2);
        dictionary["correlationid"].Should().Be("abc");
        dictionary["requestid"].Should().Be("def");
    }

    [Fact]
    public void NewFromPairs_WhenNull_ShouldThrow()
    {
        IEnumerable<KeyValuePair<string, object>> pairs = null!;

        AssertArgumentNull(
            () => new DefaultMetadata(pairs),
            "pairs",
            MetadataOutcome.Failure.Dictionary.Argument.Null.Representation);
    }

    [Fact]
    public void NewFromPairs_WhenKeyNull_ShouldThrow()
    {
        var pairs = new[]
        {
        new KeyValuePair<string, object>(null!, "value")
    };

        var exception = ((Action)(() => new DefaultMetadata(pairs)))
            .Should()
            .Throw<ArgumentException>()
            .Which;

        exception.ParamName.Should().Be("pairs");
        exception.Message.Should().Contain(
            MetadataOutcome.Failure.Key.Argument.Null.Representation);
    }

    [Fact]
    public void NewFromPairs_WhenKeyDuplicated_ShouldKeepLastValue()
    {
        var pairs = new[]
        {
        new KeyValuePair<string, object>("a", 1),
        new KeyValuePair<string, object>("a", 2)
    };

        var dictionary = new DefaultMetadata(pairs);

        dictionary.Count.Should().Be(1);
        dictionary["a"].Should().Be(2);
    }

    [Fact]
    public void NewFromPairs_WhenEmpty_ShouldCreateEmptyStore()
    {
        var dictionary = new DefaultMetadata(
            Array.Empty<KeyValuePair<string, object>>());

        dictionary.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldReturnFreshInstance()
    {
        var first = DefaultMetadata.Create();
        var second = DefaultMetadata.Create();

        first.Should().NotBeSameAs(second);
        first.Should().NotBeSameAs(DefaultMetadata.Empty);
        second.Should().NotBeSameAs(DefaultMetadata.Empty);
    }

    [Fact]
    public void CreateFromDictionary_ShouldCopyEntries()
    {
        var source = new Dictionary<string, object>
        {
            ["RequestId"] = 1
        };

        var dictionary = DefaultMetadata.Create(source);

        dictionary.Count.Should().Be(1);
        dictionary["requestid"].Should().Be(1);
    }

    [Fact]
    public void CreateFromDictionary_WhenNull_ShouldThrow()
    {
        IDictionary<string, object> source = null!;

        AssertArgumentNull(
            () => DefaultMetadata.Create(source),
            "dictionary",
            MetadataOutcome.Failure.Dictionary.Argument.Null.Representation);
    }

    [Fact]
    public void CreateFromPairs_WhenNull_ShouldThrow()
    {
        IEnumerable<KeyValuePair<string, object>> pairs = null!;

        AssertArgumentNull(
            () => DefaultMetadata.Create(pairs),
            "pairs",
            MetadataOutcome.Failure.Dictionary.Argument.Null.Representation);
    }

    [Fact]
    public void Empty_ShouldBeSharedAndEmpty()
    {
        var first = DefaultMetadata.Empty;
        var second = DefaultMetadata.Empty;

        first.Should().BeSameAs(second);
        first.Should().BeEmpty();
    }

    [Fact]
    public void With_ShouldReturnSameInstanceAndAddValue()
    {
        var dictionary = DefaultMetadata.Create();

        var result = dictionary.With("a", 1);

        result.Should().BeSameAs(dictionary);
        dictionary["a"].Should().Be(1);
    }

    [Fact]
    public void With_ShouldOverwriteCaseInsensitively()
    {
        var dictionary = DefaultMetadata.Create()
            .With("alpha", 1);

        dictionary.With("ALPHA", 2);

        dictionary.Count.Should().Be(1);
        dictionary["Alpha"].Should().Be(2);
    }

    [Fact]
    public void With_WhenKeyNull_ShouldThrow()
    {
        var dictionary = DefaultMetadata.Create();

        AssertArgumentNull(
            () => dictionary.With(null!, 1),
            "key",
            MetadataOutcome.Failure.Key.Argument.Null.Representation);
    }

    [Fact]
    public void With_WhenValueNull_ShouldThrow()
    {
        var dictionary = DefaultMetadata.Create();

        AssertArgumentNull(
            () => dictionary.With("a", null!),
            "value");
    }

    [Fact]
    public void Metadata_ShouldImplementMetadataContract()
    {
        var dictionary = new DefaultMetadata();

        dictionary.Should().BeAssignableTo<IMetadata>();
        dictionary.Should().BeAssignableTo<IReadOnlyDictionary<string, object>>();
    }

    private static void AssertArgumentNull(
        Action act,
        string expectedParamName,
        string? expectedMessage = null)
    {
        var exception = act.Should()
            .Throw<ArgumentNullException>()
            .Which;

        exception.ParamName.Should().Be(expectedParamName);

        if (expectedMessage is not null)
        {
            exception.Message.Should().Contain(expectedMessage);
        }
    }

}
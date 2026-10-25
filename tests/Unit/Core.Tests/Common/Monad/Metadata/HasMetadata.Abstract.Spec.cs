namespace Core.UnitTests.Common.Monad.Metadata;

/// <summary>
/// Tests for <see cref="HasMetadataBase"/>: every carrier owns its own metadata
/// dictionary (the shared <see cref="DefaultMetadata.Empty"/> sentinel must never
/// become a per-object default).
/// </summary>
[Trait("Category", "Unit")]
public class HasMetadataAbstractSpec
{
    private sealed record Carrier : HasMetadataBase;

    [Fact]
    public void Metadata_ShouldBeANewInstancePerObject()
    {
        var first = new Carrier();
        var second = new Carrier();

        first.Metadata.Should().NotBeSameAs(second.Metadata);
        first.Metadata.Should().NotBeSameAs(DefaultMetadata.Empty);
    }

    [Fact]
    public void Metadata_ShouldNotLeakStateAcrossObjects()
    {
        var first = new Carrier();
        var second = new Carrier();

        first.SetCorrelationId();

        first.GetCorrelationId().Should().NotBeNull();
        second.GetCorrelationId().Should().BeNull();
    }
}
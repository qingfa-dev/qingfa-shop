using Testing.Fixtures;

using Xunit;

namespace Api.Tests;

/// <summary>
/// One in-process Api host shared by every suite that exercises the pipeline.
/// </summary>
/// <remarks>
/// The host itself is cheap, but a collection keeps a single
/// <see cref="ShopApiFactory"/> (and therefore a single <see cref="ShopApiFactory.Clock"/>)
/// alive for the whole run. Because <c>FakeTimeProvider</c> only accepts forward moves,
/// members of this collection must advance the clock rather than set absolute instants.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ApiHostCollection : ICollectionFixture<ShopApiFactory>
{
    /// <summary>The collection name every Api pipeline test opts into.</summary>
    public const string Name = "api-host";
}
namespace Api.Integration.Tests;

/// <summary>One in-process Api host with a deterministic identity service.</summary>
/// <remarks>
/// The factory publishes process-wide connection strings, so this collection opts out of
/// parallel execution with the other Api hosts.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IdentityApiCollection : ICollectionFixture<IdentityApiFactory>
{
    /// <summary>The collection name the identity route tests opt into.</summary>
    public const string Name = "identity-api";
}

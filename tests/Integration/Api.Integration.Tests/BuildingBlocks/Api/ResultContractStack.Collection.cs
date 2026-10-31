namespace Api.Integration.Tests.BuildingBlocks.Api;

/// <summary>One container-backed Api host for the result-contract flow tests.</summary>
/// <remarks>
/// The factory publishes its connection string through process environment variables,
/// so this collection opts out of parallel execution with the container-free hosts.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ResultContractStackCollection : ICollectionFixture<ResultContractStackFixture>
{
    /// <summary>The collection name the flow tests opt into.</summary>
    public const string Name = "result-contract-stack";
}

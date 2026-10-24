using Testing.Fixtures;

using Xunit;

namespace Testing;

/// <summary>
/// Base class for tests that need a booted Api host.
/// </summary>
/// <remarks>
/// The host is supplied by the consuming test collection's fixture rather than created
/// per test, so the pipeline boots once and the derived suites stay free of host setup.
/// Only the HTTP client is owned here; the factory is owned by that fixture.
/// </remarks>
public abstract class IntegrationTestBase : IDisposable
{
    /// <summary>The shared factory under test; exposes <c>Clock</c> for time assertions.</summary>
    protected ShopApiFactory Factory { get; }

    /// <summary>An HTTP client bound to the in-process host.</summary>
    protected HttpClient Client { get; }

    /// <summary>Binds a client to the collection's shared host.</summary>
    protected IntegrationTestBase(ShopApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
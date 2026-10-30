using System.Net.Http.Headers;

using Application.Common.Persistence.Initialization;
using Application.Common.Security.Authentication.Tokens;
using Application.Common.Security.Authentication.Tokens.Abstractions.Jwt;

using Core.Common.Monad.Results;

using Infrastructure.Persistence.EntityFramework.Contexts;
using Infrastructure.Persistence.EntityFramework.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testing;
using Testing.Fixtures;

using Testcontainers.PostgreSql;

namespace Api.Integration.Tests;

/// <summary>
/// Boots one Api host over a real PostgreSQL container so contract tests can exercise
/// authenticated writes and deletes against the production persistence pipeline.
/// </summary>
/// <remarks>
/// The fast <see cref="ApiHostCollection"/> host is not backed by a live database; this
/// fixture is separate so only the flow tests pay for a container. Tests call
/// <see cref="Assert.Skip(string)"/> through <see cref="SkipReason"/> when no engine
/// is available, keeping container-free runs green.
/// </remarks>
public sealed class ResultContractStackFixture : IAsyncLifetime
{
    private const string Image = "postgres:17-alpine";

    private PostgreSqlContainer? _container;

    /// <summary>The host under test; unavailable when <see cref="SkipReason"/> is set.</summary>
    public IdentityStackFactory Factory { get; private set; } = null!;

    /// <summary>The reason container-backed tests must skip, or <c>null</c> when ready.</summary>
    public string? SkipReason { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            SkipReason = ContainerRuntime.SkipReason;
            return;
        }

        ContainerRuntime.ConfigureProcessEnvironment();

        _container = new PostgreSqlBuilder(Image)
            .WithDatabase("qingfa_contract")
            .WithUsername("qingfa")
            .WithPassword("qingfa")
            .Build();

        await _container.StartAsync();

        string connectionString = _container.GetConnectionString();
        await MigrateAsync(connectionString);

        // Initialize: the factory publishes the connection string as ConnectionStrings__db.
        // Development config names the "Default" string, so point EF Core at "db" the same
        // way AppHost does before the host composes the context.
        Environment.SetEnvironmentVariable("EfCore__ConnectionStringName", "db");

        // Initialize: the factory publishes the connection string before Program composes
        // the context, so the host and the pre-migrated database point at the same server.
        Factory = new IdentityStackFactory { DatabaseConnectionString = connectionString };
        Factory.Clock.SetUtcNow(DateTimeOffset.UtcNow);
        _ = Factory.Services;

        await SeedIdentityAsync();
    }

    /// <summary>Creates a client carrying a signed token with the supplied roles.</summary>
    /// <param name="roles">Roles embedded in the token; <c>Admin</c> bypasses permissions.</param>
    /// <returns>A client bound to the shared host.</returns>
    public HttpClient CreateClient(params string[] roles)
    {
        ITokenService tokenService = Factory.Services.GetRequiredService<ITokenService>();
        Result<AccessToken> issued = tokenService.CreateAccessToken(new AccessTokenRequest(
            UserId: Guid.NewGuid().ToString(),
            TenantId: Guid.NewGuid(),
            Roles: roles));

        if (issued.IsFailure)
            throw new InvalidOperationException(issued.Errors[0].Message);

        HttpClient client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", issued.Value.Token);

        return client;
    }

    /// <summary>Runs the identity seeders against the migrated database.</summary>
    /// <exception cref="InvalidOperationException">A seeder reported a failure.</exception>
    public async Task SeedIdentityAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();

        IPersistenceInitializer initializer = scope.ServiceProvider
            .GetRequiredService<IPersistenceInitializer>();

        Result seeded = await initializer.InitializeAsync(
            runMigrations: false,
            runSeeders: true,
            cancellationToken: TestContext.Current.CancellationToken);

        if (seeded.IsFailure)
        {
            throw new InvalidOperationException(
                $"Identity seeding failed: {string.Join("; ", seeded.Errors.Select(error => error.Message))}");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        if (_container is not null)
            await _container.DisposeAsync();
    }

    private static async Task MigrateAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UsePersistenceMigrationHistory())
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }
}

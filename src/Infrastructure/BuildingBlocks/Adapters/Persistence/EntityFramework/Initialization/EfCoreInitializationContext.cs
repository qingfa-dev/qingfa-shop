using System.Data;

using Application.BuildingBlocks.Persistence.Initialization;
using Application.BuildingBlocks.Persistence.UnitOfWorks;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Initialization;

/// <summary>
/// EF Core implementation of <see cref="IInitializationContext"/>.
/// Provides database access, logging, and cross-step state for initialization steps.
/// </summary>
public sealed class EfCoreInitializationContext : IInitializationContext
{
    /// <inheritdoc />
    public IDbConnection Connection { get; }

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork { get; }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Items { get; }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Creates a new initialization context.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="unitOfWork">The unit of work for transactional operations.</param>
    /// <param name="logger">The logger for this initialization run.</param>
    /// <param name="cancellationToken">The cancellation token for the entire initialization.</param>
    public EfCoreInitializationContext(
        IDbConnection connection,
        IUnitOfWork unitOfWork,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CancellationToken = cancellationToken;
        Items = new Dictionary<string, object>();
    }
}

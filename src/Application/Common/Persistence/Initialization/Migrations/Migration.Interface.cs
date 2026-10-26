namespace Application.Common.Persistence.Initialization.Migrations;

public interface IPersistenceMigrator
{
    string Name { get; }

    int Order { get; }

    Task<Result> MigrateAsync(
        CancellationToken cancellationToken = default);

    bool IsTransient(Exception exception);
}
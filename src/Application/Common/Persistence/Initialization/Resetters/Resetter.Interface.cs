namespace Application.Common.Persistence.Initialization.Resetters;

public interface IPersistenceResetter
{
    Task<Result> ResetAsync(
        CancellationToken cancellationToken = default);
}
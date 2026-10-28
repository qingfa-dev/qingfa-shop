using Core.Common.Monad.Results;

namespace Contract.Common;

/// <summary>
/// Composes create, update, and projection delegates into result-returning entity flows.
/// </summary>
public static class EntityMapperExtensions
{
    #region Create

    /// <summary>Maps create parameters to a new domain entity.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The create parameters type.</typeparam>
    /// <param name="parameters">The create parameters.</param>
    /// <param name="create">Creates the entity from the parameters.</param>
    /// <returns>The result produced by <paramref name="create"/>.</returns>
    public static Result<TEntity> MapToDomain<TEntity, TParameters>(
        this TParameters parameters,
        Func<TParameters, Result<TEntity>> create)
        where TEntity : class
        where TParameters : class
        => create(parameters);

    /// <summary>Maps create parameters to a new entity when the caller does not need the created entity.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The create parameters type.</typeparam>
    /// <param name="parameters">The create parameters.</param>
    /// <param name="create">Creates the entity from the parameters.</param>
    /// <returns>The success or failure of <paramref name="create"/> without the created entity.</returns>
    public static Result MapToDomainAndForget<TEntity, TParameters>(
        this TParameters parameters,
        Func<TParameters, Result<TEntity>> create)
        where TEntity : class
        where TParameters : class
        => create(parameters);

    /// <summary>Creates an entity from the parameters and projects it to a response.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The create parameters type.</typeparam>
    /// <typeparam name="TResponse">The response type derived from the parameters.</typeparam>
    /// <param name="parameters">The create parameters.</param>
    /// <param name="create">Creates the entity from the parameters.</param>
    /// <param name="fill">Copies entity values into the response.</param>
    /// <returns>The projected response, or the failure from <paramref name="create"/>.</returns>
    public static Result<TResponse> MapToDomain<TEntity, TParameters, TResponse>(
        this TParameters parameters,
        Func<TParameters, Result<TEntity>> create,
        Action<TEntity, TResponse> fill)
        where TEntity : class
        where TParameters : class
        where TResponse : TParameters, new()
        => create(parameters).Map(e => e.MapToResponse<TEntity, TParameters, TResponse>(fill));

    #endregion

    #region Update

    /// <summary>Applies update parameters to an existing entity.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The update parameters type.</typeparam>
    /// <param name="parameters">The update parameters.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="update">Applies the parameters to the entity.</param>
    /// <returns>The success or failure of <paramref name="update"/>.</returns>
    public static Result MapToDomain<TEntity, TParameters>(
        this TParameters parameters,
        TEntity entity,
        Func<TEntity, TParameters, Result> update)
        where TEntity : class
        where TParameters : class
        => update(entity, parameters);

    /// <summary>Applies update parameters and returns the mutated entity.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The update parameters type.</typeparam>
    /// <param name="parameters">The update parameters.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="update">Applies the parameters to the entity.</param>
    /// <returns>The mutated entity, or the failure from <paramref name="update"/>.</returns>
    public static Result<TEntity> MapToDomainAndReturn<TEntity, TParameters>(
        this TParameters parameters,
        TEntity entity,
        Func<TEntity, TParameters, Result> update)
        where TEntity : class
        where TParameters : class
        => update(entity, parameters).Map(() => entity);

    /// <summary>Applies update parameters and projects the mutated entity to a response.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The update parameters type.</typeparam>
    /// <typeparam name="TResponse">The response type derived from the parameters.</typeparam>
    /// <param name="parameters">The update parameters.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="update">Applies the parameters to the entity.</param>
    /// <param name="fill">Copies entity values into the response.</param>
    /// <returns>The projected response, or the failure from <paramref name="update"/>.</returns>
    public static Result<TResponse> MapToDomain<TEntity, TParameters, TResponse>(
        this TParameters parameters,
        TEntity entity,
        Func<TEntity, TParameters, Result> update,
        Action<TEntity, TResponse> fill)
        where TEntity : class
        where TParameters : class
        where TResponse : TParameters, new()
        => update(entity, parameters)
            .Map(() => entity.MapToResponse<TEntity, TParameters, TResponse>(fill));

    #endregion

    #region Project

    /// <summary>Projects one entity to a new response.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The parameter base type of the response.</typeparam>
    /// <typeparam name="TResponse">The response type derived from the parameters.</typeparam>
    /// <param name="entity">The entity to project.</param>
    /// <param name="fill">Copies entity values into the response.</param>
    /// <returns>The projected response.</returns>
    public static TResponse MapToResponse<TEntity, TParameters, TResponse>(
        this TEntity entity,
        Action<TEntity, TResponse> fill)
        where TEntity : class
        where TParameters : class
        where TResponse : TParameters, new()
    {
        var response = new TResponse();
        fill(entity, response);
        return response;
    }

    /// <summary>Projects every entity in the sequence to a response.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The parameter base type of the response.</typeparam>
    /// <typeparam name="TResponse">The response type derived from the parameters.</typeparam>
    /// <param name="entities">The entities to project.</param>
    /// <param name="fill">Copies entity values into each response.</param>
    /// <returns>The projected responses in source order.</returns>
    public static IReadOnlyList<TResponse> MapToResponses<TEntity, TParameters, TResponse>(
        this IEnumerable<TEntity> entities,
        Action<TEntity, TResponse> fill)
        where TEntity : class
        where TParameters : class
        where TResponse : TParameters, new()
        => entities.Select(e => e.MapToResponse<TEntity, TParameters, TResponse>(fill)).ToList();

    /// <summary>Projects every entity in a page while preserving the paging metadata.</summary>
    /// <typeparam name="TEntity">The domain entity type.</typeparam>
    /// <typeparam name="TParameters">The parameter base type of the response.</typeparam>
    /// <typeparam name="TResponse">The response type derived from the parameters.</typeparam>
    /// <param name="page">The page of entities to project.</param>
    /// <param name="fill">Copies entity values into each response.</param>
    /// <returns>A page of projected responses with the original paging metadata.</returns>
    public static PageModel<TResponse> MapToResponses<TEntity, TParameters, TResponse>(
        this PageModel<TEntity> page,
        Action<TEntity, TResponse> fill)
        where TEntity : class
        where TParameters : class
        where TResponse : TParameters, new()
        => new PageModel<TResponse>(
            Items: page.Items
                            .Select(e => e.MapToResponse<TEntity, TParameters, TResponse>(fill))
                            .ToList(),
            Page: page.Page,
            PageSize: page.PageSize,
            TotalCount: page.TotalCount);

    #endregion
}
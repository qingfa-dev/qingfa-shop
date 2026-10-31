using Application.BuildingBlocks.Persistence.UnitOfWorks;
using Application.BuildingBlocks.Security.Authentication.Tokens.Abstractions.Refresh;
using Application.BuildingBlocks.Security.Authentication.Tokens.Domain;

using Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.Contexts;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.BuildingBlocks.Adapters.Security.Authentication.Tokens.Stores;

/// <summary>EF Core adapter for the application refresh-token store port.</summary>
public sealed class EfCoreRefreshTokenStore(
    ApplicationDbContext dbContext,
    IUnitOfWork unitOfWork) : IRefreshTokenStore
{
    /// <inheritdoc />
    public async Task<Result<Maybe<RefreshToken>>> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        RefreshToken? token = await dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        return Result<Maybe<RefreshToken>>.Ok(
            token is null ? Maybe<RefreshToken>.Empty() : Maybe<RefreshToken>.From(token));
    }

    /// <inheritdoc />
    public async Task<Result> AddAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Set<RefreshToken>().AddAsync(token, cancellationToken).ConfigureAwait(false);
        return await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default)
    {
        dbContext.Set<RefreshToken>().Update(token);
        return await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> RotateAsync(
        RefreshToken current,
        RefreshToken replacement,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Set<RefreshToken>().AddAsync(replacement, cancellationToken).ConfigureAwait(false);
        dbContext.Set<RefreshToken>().Update(current);
        return await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<int>> RevokeAllAsync(
        string userId,
        DateTimeOffset revokedAtUtc,
        RefreshTokenRevocationReason reason,
        CancellationToken cancellationToken = default)
    {
        List<RefreshToken> tokens = await dbContext.Set<RefreshToken>()
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (RefreshToken token in tokens)
        {
            Result<RefreshToken> revoked = Result<RefreshToken>
                .Success(token)
                .Revoke(revokedAtUtc, reason);

            if (revoked.IsFailure)
                return Result<int>.Fail(revoked.Errors);
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return saved.IsSuccess
            ? Result<int>.Ok(tokens.Count)
            : Result<int>.Fail(saved.Errors);
    }
}

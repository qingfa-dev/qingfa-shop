using Application.BuildingBlocks.Security.Authentication.Outcome;

namespace Application.BuildingBlocks.Security.Identity.Auth;

/// <summary>Resolves the authenticated caller required by the current-user use cases.</summary>
public static class AuthGuard
{
    /// <summary>Resolves the authenticated user identifier or a 401 failure.</summary>
    /// <param name="context">The request user context.</param>
    /// <returns>The user identifier, or an unauthorized failure when the claim is missing.</returns>
    public static Result<Guid> RequireUserId(IUserContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Guid.TryParse(context.UserId, out Guid userId)
            ? Result<Guid>.Ok(userId)
            : Result<Guid>.Fail(AuthenticationOutcome.Failure.UserIdMissing);
    }
}

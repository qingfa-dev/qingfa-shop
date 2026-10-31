namespace Infrastructure.BuildingBlocks.Adapters.Security.Identity.Domain.Roles;

/// <summary>Monadic role mutations.</summary>
public static class RoleExtension
{
    /// <summary>Updates a mutable role.</summary>
    /// <param name="result">The result carrying the role.</param>
    /// <param name="name">The optional replacement name.</param>
    /// <param name="description">The optional replacement description.</param>
    /// <returns>The updated result, or the first failure.</returns>
    public static Result<Role> Update(
        this Result<Role> result,
        string? name = null,
        string? description = null)
    {
        return result
            .Ensure(role => !role.IsSystem, RoleOutcome.Failure.SystemRoleProtected)
            .Ensure(_ => name is null
                ? Result.Ok()
                : RoleValidator.ValidateName(name))
            .Ensure(_ => description is null
                ? Result.Ok()
                : RoleValidator.ValidateDescription(description))
            .Tap(role => ApplyUpdate(role, name, description));
    }

    /// <summary>Deletes a mutable role that has no assigned users.</summary>
    /// <param name="result">The result carrying the role.</param>
    /// <returns>The deleted role on success; otherwise a failure.</returns>
    public static Result<Role> Delete(this Result<Role> result)
    {
        return result
            .Ensure(role => !role.IsSystem, RoleOutcome.Failure.SystemRoleProtected)
            .Ensure(role => role.UserRoles.Count == 0, RoleOutcome.Failure.CannotDeleteRoleWithUsers)
            .Tap(role => role.RaiseEvent(new RoleDeleted(role.Id)));
    }

    private static void ApplyUpdate(Role role, string? name, string? description)
    {
        bool changed = false;

        if (name is not null && name.Trim() != role.Name)
        {
            role.Name = name.Trim();
            changed = true;
        }

        if (description is not null && description.Trim() != role.Description)
        {
            role.Description = description.Trim();
            changed = true;
        }

        if (changed)
        {
            role.ModifiedAtUtc = DateTimeOffset.UtcNow;
            role.ModifiedBy = "System";
            role.RaiseEvent(new RoleUpdated(role.Id, role.Name!));
        }
    }
}

namespace Infrastructure.Security.Identity.Domain.Roles;

/// <summary>Validates role invariants without mutating the entity.</summary>
public static class RoleValidator
{
    /// <summary>Validates the role's stable fields.</summary>
    /// <param name="role">The role to validate.</param>
    /// <returns>The unchanged role on success, or the first broken rule.</returns>
    public static Result<Role> Validate(Role? role)
    {
        if (role is null)
            return Result<Role>.Fail(RoleOutcome.Failure.EntityRequired);

        return Result<Role>.Success(role)
            .Ensure(candidate => ValidateName(candidate.Name))
            .Ensure(candidate => ValidateDescription(candidate.Description));
    }

    /// <summary>Validates a role name.</summary>
    /// <param name="name">The role name to validate.</param>
    /// <returns>Success when the name is usable; otherwise a failure.</returns>
    public static Result ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail(RoleOutcome.Failure.NameRequired);

        return name.Length > RoleConstant.Constraints.Name.MaxLength
            ? Result.Fail(RoleOutcome.Failure.NameTooLong)
            : Result.Ok();
    }

    /// <summary>Validates an optional role description.</summary>
    /// <param name="description">The description to validate.</param>
    /// <returns>Success when the description is usable; otherwise a failure.</returns>
    public static Result ValidateDescription(string? description)
    {
        if (description is not null &&
            description.Length > RoleConstant.Constraints.Description.MaxLength)
        {
            return Result.Fail(RoleOutcome.Failure.DescriptionTooLong);
        }

        return Result.Ok();
    }
}

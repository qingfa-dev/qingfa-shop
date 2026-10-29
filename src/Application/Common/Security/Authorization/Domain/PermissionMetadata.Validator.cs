using System.Linq.Expressions;

using FluentValidation;

namespace Application.Common.Security.Authorization.Domain;

/// <summary>
/// Validates canonical permission metadata.
/// </summary>
public sealed class PermissionMetadataValidator : AbstractValidator<PermissionMetadata>
{
    /// <summary>Creates the validator with the canonical part and display rules.</summary>
    public PermissionMetadataValidator()
    {
        RuleForPart(x => x.Domain, nameof(PermissionMetadata.Domain));
        RuleForPart(x => x.Category, nameof(PermissionMetadata.Category));
        RuleForPart(x => x.Resource, nameof(PermissionMetadata.Resource));
        RuleForPart(x => x.Action, nameof(PermissionMetadata.Action));

        RuleFor(x => x.Name)
            .MaximumLength(PermissionMetadataConstant.Constraints.MaxNameLength)
            .WithErrorCode(PermissionMetadataResult.Code("Name", "TooLong"))
            .WithMessage((_, value) => PermissionMetadataResult.Failure.NameTooLong(value.Length).Message)
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(PermissionMetadataConstant.Constraints.MaxDescriptionLength)
            .WithErrorCode(PermissionMetadataResult.Code("Description", "TooLong"))
            .WithMessage((_, value) => PermissionMetadataResult.Failure.DescriptionTooLong(value.Length).Message)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithErrorCode(PermissionMetadataResult.Failure.IdentifierRequired.Code)
            .WithMessage(PermissionMetadataResult.Failure.IdentifierRequired.Message)
            .MaximumLength(PermissionMetadataConstant.Constraints.MaxIdentifierLength)
            .WithErrorCode(PermissionMetadataResult.Code("Identifier", "TooLong"))
            .WithMessage((_, value) => PermissionMetadataResult.Failure.IdentifierTooLong(value.Length).Message)
            .Matches(PermissionMetadataConstant.IdentifierPattern)
            .WithErrorCode(PermissionMetadataResult.Code("Identifier", "InvalidFormat"))
            .WithMessage((_, value) => PermissionMetadataResult.Failure.InvalidIdentifierFormat(value).Message);
    }

    private void RuleForPart(
        Expression<Func<PermissionMetadata, string>> property,
        string partName)
    {
        RuleFor(property)
            .NotEmpty()
            .WithErrorCode(PermissionMetadataResult.Code(partName, "Required"))
            .WithMessage(PermissionMetadataResult.Failure.PartRequired(partName).Message)
            .MaximumLength(PermissionMetadataConstant.Constraints.MaxPartLength)
            .WithErrorCode(PermissionMetadataResult.Code(partName, "TooLong"))
            .WithMessage((_, value) => PermissionMetadataResult.Failure.PartTooLong(partName, value.Length).Message)
            .Matches(PermissionMetadataConstant.PartPattern)
            .WithErrorCode(PermissionMetadataResult.Code(partName, "InvalidChars"))
            .WithMessage((_, value) => PermissionMetadataResult.Failure.InvalidPartChars(partName, value).Message);
    }
}

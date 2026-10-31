using System.Linq.Expressions;

using FluentValidation;

namespace Application.BuildingBlocks.Security.Authorization.Domain;

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
            .WithErrorCode(PermissionMetadataOutcome.Code("Name", "TooLong"))
            .WithMessage((_, value) => PermissionMetadataOutcome.Failure.NameTooLong(value.Length).Message)
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(PermissionMetadataConstant.Constraints.MaxDescriptionLength)
            .WithErrorCode(PermissionMetadataOutcome.Code("Description", "TooLong"))
            .WithMessage((_, value) => PermissionMetadataOutcome.Failure.DescriptionTooLong(value.Length).Message)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithErrorCode(PermissionMetadataOutcome.Failure.IdentifierRequired.Code)
            .WithMessage(PermissionMetadataOutcome.Failure.IdentifierRequired.Message)
            .MaximumLength(PermissionMetadataConstant.Constraints.MaxIdentifierLength)
            .WithErrorCode(PermissionMetadataOutcome.Code("Identifier", "TooLong"))
            .WithMessage((_, value) => PermissionMetadataOutcome.Failure.IdentifierTooLong(value.Length).Message)
            .Matches(PermissionMetadataConstant.IdentifierPattern)
            .WithErrorCode(PermissionMetadataOutcome.Code("Identifier", "InvalidFormat"))
            .WithMessage((_, value) => PermissionMetadataOutcome.Failure.InvalidIdentifierFormat(value).Message);
    }

    private void RuleForPart(
        Expression<Func<PermissionMetadata, string>> property,
        string partName)
    {
        RuleFor(property)
            .NotEmpty()
            .WithErrorCode(PermissionMetadataOutcome.Code(partName, "Required"))
            .WithMessage(PermissionMetadataOutcome.Failure.PartRequired(partName).Message)
            .MaximumLength(PermissionMetadataConstant.Constraints.MaxPartLength)
            .WithErrorCode(PermissionMetadataOutcome.Code(partName, "TooLong"))
            .WithMessage((_, value) => PermissionMetadataOutcome.Failure.PartTooLong(partName, value.Length).Message)
            .Matches(PermissionMetadataConstant.PartPattern)
            .WithErrorCode(PermissionMetadataOutcome.Code(partName, "InvalidChars"))
            .WithMessage((_, value) => PermissionMetadataOutcome.Failure.InvalidPartChars(partName, value).Message);
    }
}

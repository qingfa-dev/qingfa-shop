using FluentValidation;

namespace Application.BuildingBlocks.Security.Authorization.Options;

public sealed partial class AuthzSettingValidator : AbstractValidator<AuthzSetting>
{

    public AuthzSettingValidator()
    {
        RuleFor(x => x.PermissionCache).NotNull();

        RuleFor(x => x.PermissionCache.SlidingExpiration)
            .GreaterThanOrEqualTo(TimeSpan.FromMinutes(AuthzSettingConstant.Constraints.PermissionCacheSlidingExpirationMin))
            .WithErrorCode(AuthzSettingOutcome.Failure.PermissionCacheSlidingExpirationPositive.Code)
            .WithMessage(AuthzSettingOutcome.Failure.PermissionCacheSlidingExpirationPositive.Message)
            .When(x => x.PermissionCache != null);

        RuleFor(x => x.PermissionCache.AbsoluteExpiration)
            .GreaterThanOrEqualTo(TimeSpan.FromMinutes(AuthzSettingConstant.Constraints.PermissionCacheAbsoluteExpirationMin))
            .WithErrorCode(AuthzSettingOutcome.Failure.PermissionCacheAbsoluteExpirationPositive.Code)
            .WithMessage(AuthzSettingOutcome.Failure.PermissionCacheAbsoluteExpirationPositive.Message)
            .When(x => x.PermissionCache != null);

        RuleFor(x => x.PermissionCache)
            .Must(cache => cache.SlidingExpiration <= cache.AbsoluteExpiration)
            .WithErrorCode(AuthzSettingOutcome.Failure.PermissionCacheLocalExceedsAbsolute.Code)
            .WithMessage(AuthzSettingOutcome.Failure.PermissionCacheLocalExceedsAbsolute.Message)
            .When(x => x.PermissionCache != null);
    }
}

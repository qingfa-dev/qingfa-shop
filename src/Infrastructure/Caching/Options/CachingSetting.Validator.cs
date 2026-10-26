using Infrastructure.Caching.Options;

using Microsoft.Extensions.Options;

namespace Infrastructure.Caching;

/// <summary>Adapts <see cref="CachingSettingValidation"/> to the options pipeline.</summary>
public sealed class CachingSettingValidator : IValidateOptions<CachingSetting>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CachingSetting options)
    {
        Result<CachingSetting> result = CachingSettingValidation.Validate(options);

        return result.IsSuccess
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(CachingSettingValidation.Describe(result));
    }
}
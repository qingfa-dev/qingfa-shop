using Core.Common.Concerns.Content.Localizable;

namespace Core.UnitTests.Common.Concerns.Content.Localizable;

/// <summary>Spec for <see cref="LocalizableExtensions"/> covering extension behavior,
/// <see cref="LocalizableValidator"/> branches and <see cref="LocalizableConstant"/> values.</summary>
public class LocalizableExtensionSpec
{
    #region Helper Methods

    private sealed class TestLocalizable : ILocalizable
    {
        public ICollection<LocalizedValue> LocalizedValues { get; } =
            new List<LocalizedValue>();
    }

    /// <summary>Creates a successful result wrapping the test entity.</summary>
    /// <param name="entity">The test entity.</param>
    /// <returns>A successful result.</returns>
    private static Result<TestLocalizable> Success(
        TestLocalizable entity)
    {
        return Result<TestLocalizable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void SetLocalizedValue_NewEntry_ShouldAddEntry()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = Success(entity)
            .SetLocalizedValue("title", "en", "Hello");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.LocalizedValues.Should().ContainSingle();
        entity.LocalizedValues.Single().Property.Should().Be("title");
        entity.LocalizedValues.Single().Locale.Should().Be("en");
        entity.LocalizedValues.Single().Value.Should().Be("Hello");
    }

    [Fact]
    public void SetLocalizedValue_ExistingEntry_ShouldUpdateValue()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Old");

        // Act:
        var result = Success(entity)
            .SetLocalizedValue("title", "en", "New");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.LocalizedValues.Should().ContainSingle();
        entity.LocalizedValues.Single().Value.Should().Be("New");
    }

    [Fact]
    public void SetLocalizedValue_NullProperty_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = Success(entity)
            .SetLocalizedValue(null, "en", "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Property.Required");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void SetLocalizedValue_TooLongProperty_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestLocalizable();
        var property = new string(
            'p',
            LocalizableConstant.Constraints.Property.MaxLength + 1);

        // Act:
        var result = Success(entity)
            .SetLocalizedValue(property, "en", "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Property.TooLong");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void SetLocalizedValue_NullLocale_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = Success(entity)
            .SetLocalizedValue("title", null, "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Locale.Required");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void SetLocalizedValue_TooLongLocale_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestLocalizable();
        var locale = new string(
            'l',
            LocalizableConstant.Constraints.Locale.MaxLength + 1);

        // Act:
        var result = Success(entity)
            .SetLocalizedValue("title", locale, "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Locale.TooLong");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLocalizedValue_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            (TestLocalizable)null!,
            "title",
            "en",
            "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Entity.Required");
    }

    [Fact]
    public void RemoveLocalizedValue_ExistingEntry_ShouldRemoveEntry()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");

        // Act:
        var result = Success(entity)
            .RemoveLocalizedValue("title", "en");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLocalizedValue_MissingEntry_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");

        // Act:
        var result = Success(entity)
            .RemoveLocalizedValue("title", "fr");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Value.NotFound");
        entity.LocalizedValues.Should().ContainSingle();
    }

    [Fact]
    public void ClearLocalizedValues_WithEntries_ShouldClearEntries()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");
        Success(entity).SetLocalizedValue("title", "fr", "Bonjour");

        // Act:
        var result = Success(entity).ClearLocalizedValues();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void ClearLocalizedValues_EmptyCollection_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = Success(entity).ClearLocalizedValues();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void SetLocalizedValue_NullValue_ShouldSucceedWithNullStored()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = Success(entity).SetLocalizedValue("title", "en", null);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        var entry = entity.LocalizedValues.Should().ContainSingle().Which;
        entry.Value.Should().BeNull();
    }

    [Fact]
    public void SetLocalizedValue_PaddedInputs_ShouldNormalizeBeforeStore()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = Success(entity).SetLocalizedValue("  title  ", "  en  ", "Hello");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        var entry = entity.LocalizedValues.Should().ContainSingle().Which;
        entry.Property.Should().Be("title");
        entry.Locale.Should().Be("en");
    }

    [Fact]
    public void SetLocalizedValue_UpdateExistingWhenFull_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestLocalizable();
        for (var i = 0; i < LocalizableConstant.Constraints.Collection.MaxTranslations; i++)
        {
            entity.LocalizedValues.Add(new LocalizedValue
            {
                Property = $"prop{i}",
                Locale = "en",
                Value = "Hello"
            });
        }

        // Act:
        var result = Success(entity).SetLocalizedValue("prop0", "en", "Updated");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.LocalizedValues.Count.Should().Be(
            LocalizableConstant.Constraints.Collection.MaxTranslations);
        entity.LocalizedValues.Single(e => e.Property == "prop0").Value.Should().Be("Updated");
    }

    [Fact]
    public void SetLocalizedValue_InputFailure_ShouldPropagateWithoutMutation()
    {
        // Arrange:
        var entity = new TestLocalizable();
        var input = Result<TestLocalizable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.SetLocalizedValue("title", "en", "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void GetLocalizedValue_ExistingEntry_ShouldReturnValue()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");

        // Act:
        var value = entity.GetLocalizedValue("title", "en");

        // Assert:
        value.Should().Be("Hello");
    }

    [Fact]
    public void GetLocalizedValue_MissingEntry_ShouldReturnNull()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");

        // Act:
        var value = entity.GetLocalizedValue("title", "fr");

        // Assert:
        value.Should().BeNull();
    }

    [Fact]
    public void GetLocalizedValue_NullEntity_ShouldReturnNull()
    {
        // Arrange:
        TestLocalizable entity = null!;

        // Act:
        var value = entity.GetLocalizedValue("title", "en");

        // Assert:
        value.Should().BeNull();
    }

    [Fact]
    public void GetLocalizedValue_NullProperty_ShouldReturnNull()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");

        // Act:
        var value = entity.GetLocalizedValue(null, "en");

        // Assert:
        value.Should().BeNull();
    }

    [Fact]
    public void GetLocalizedValue_NullLocale_ShouldReturnNull()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");

        // Act:
        var value = entity.GetLocalizedValue("title", null);

        // Assert:
        value.Should().BeNull();
    }

    [Fact]
    public void GetLocalizedValue_PaddedInputs_ShouldMatchNormalized()
    {
        // Arrange:
        var entity = new TestLocalizable();
        Success(entity).SetLocalizedValue("title", "en", "Hello");

        // Act:
        var value = entity.GetLocalizedValue("  title  ", "  en  ");

        // Assert:
        value.Should().Be("Hello");
    }

    [Fact]
    public void NormalizeProperty_Null_ShouldReturnNull()
    {
        // Arrange:
        // Act:
        var normalized = LocalizableExtensions.NormalizeProperty(null);

        // Assert:
        normalized.Should().BeNull();
    }

    [Fact]
    public void NormalizeProperty_Padded_ShouldTrim()
    {
        // Arrange:
        // Act:
        var normalized = LocalizableExtensions.NormalizeProperty("  title  ");

        // Assert:
        normalized.Should().Be("title");
    }

    [Fact]
    public void NormalizeLocale_Null_ShouldReturnNull()
    {
        // Arrange:
        // Act:
        var normalized = LocalizableExtensions.NormalizeLocale(null);

        // Assert:
        normalized.Should().BeNull();
    }

    [Fact]
    public void NormalizeLocale_Padded_ShouldTrim()
    {
        // Arrange:
        // Act:
        var normalized = LocalizableExtensions.NormalizeLocale("  en  ");

        // Assert:
        normalized.Should().Be("en");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateLocalizedValue_BlankProperty_ShouldFailPropertyRequired()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "   ",
            "en",
            "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Property.Required");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLocalizedValue_BlankLocale_ShouldFailLocaleRequired()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "title",
            "   ",
            "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Locale.Required");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLocalizedValue_ShortLocale_ShouldFailLocaleTooShort()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "title",
            "a",
            "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Locale.TooShort");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLocalizedValue_InvalidLocale_ShouldFailLocaleInvalid()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "title",
            "en_us!",
            "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Locale.Invalid");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLocalizedValue_TooLongValue_ShouldFailValueTooLong()
    {
        // Arrange:
        var entity = new TestLocalizable();
        var value = new string(
            'v',
            LocalizableConstant.Constraints.Value.MaxLength + 1);

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "title",
            "en",
            value);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Value.TooLong");
        entity.LocalizedValues.Should().BeEmpty();
    }

    [Fact]
    public void ValidateLocalizedValue_TooManyTranslations_ShouldFailTranslationsTooMany()
    {
        // Arrange:
        var entity = new TestLocalizable();
        for (var i = 0; i < LocalizableConstant.Constraints.Collection.MaxTranslations; i++)
        {
            entity.LocalizedValues.Add(new LocalizedValue
            {
                Property = $"prop{i}",
                Locale = "en",
                Value = "Hello"
            });
        }

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "new-prop",
            "en",
            "Hello");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Translations.TooMany");
        entity.LocalizedValues.Count.Should().Be(
            LocalizableConstant.Constraints.Collection.MaxTranslations);
    }

    [Fact]
    public void ValidateLocalizedValue_UpdateExistingWhenFull_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestLocalizable();
        for (var i = 0; i < LocalizableConstant.Constraints.Collection.MaxTranslations; i++)
        {
            entity.LocalizedValues.Add(new LocalizedValue
            {
                Property = $"prop{i}",
                Locale = "en",
                Value = "Hello"
            });
        }

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "prop0",
            "en",
            "Updated");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateLocalizedValueRemoval_MissingEntry_ShouldFailValueNotFound()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValueRemoval(
            entity,
            "title",
            "en");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Value.NotFound");
    }

    [Fact]
    public void ValidateLocalizedValueRemoval_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = LocalizableValidator.ValidateLocalizedValueRemoval(
            (TestLocalizable)null!,
            "title",
            "en");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Entity.Required");
    }

    [Fact]
    public void ValidateClearLocalizedValues_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = LocalizableValidator.ValidateClearLocalizedValues(
            (TestLocalizable)null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Localizable.Entity.Required");
    }

    [Fact]
    public void ValidateClearLocalizedValues_ExistingEntity_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestLocalizable();
        entity.LocalizedValues.Add(new LocalizedValue
        {
            Property = "title",
            Locale = "en",
            Value = "Hello"
        });

        // Act:
        var result = LocalizableValidator.ValidateClearLocalizedValues(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateLocalizedValue_NullValue_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestLocalizable();

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValue(
            entity,
            "title",
            "en",
            null);

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateLocalizedValueRemoval_ExistingEntry_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestLocalizable();
        entity.LocalizedValues.Add(new LocalizedValue
        {
            Property = "title",
            Locale = "en",
            Value = "Hello"
        });

        // Act:
        var result = LocalizableValidator.ValidateLocalizedValueRemoval(
            entity,
            "title",
            "en");

        // Assert:
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void FailureCodes_ShouldMatchDocumentedCodes()
    {
        // Arrange:
        // Act:
        // Assert:
        // Note: PropertyTooShort is shadowed by PropertyRequired for empty input
        // (MinLength 1), so its code is asserted here rather than via the validator.
        LocalizableOutcome.Failure.EntityRequired.Code.Should().Be("Localizable.Entity.Required");
        LocalizableOutcome.Failure.PropertyRequired.Code.Should().Be("Localizable.Property.Required");
        LocalizableOutcome.Failure.PropertyTooShort.Code.Should().Be("Localizable.Property.TooShort");
        LocalizableOutcome.Failure.PropertyTooLong.Code.Should().Be("Localizable.Property.TooLong");
        LocalizableOutcome.Failure.LocaleRequired.Code.Should().Be("Localizable.Locale.Required");
        LocalizableOutcome.Failure.LocaleTooShort.Code.Should().Be("Localizable.Locale.TooShort");
        LocalizableOutcome.Failure.LocaleTooLong.Code.Should().Be("Localizable.Locale.TooLong");
        LocalizableOutcome.Failure.LocaleInvalid.Code.Should().Be("Localizable.Locale.Invalid");
        LocalizableOutcome.Failure.ValueTooLong.Code.Should().Be("Localizable.Value.TooLong");
        LocalizableOutcome.Failure.ValueNotFound.Code.Should().Be("Localizable.Value.NotFound");
        LocalizableOutcome.Failure.TranslationsTooMany.Code.Should().Be("Localizable.Translations.TooMany");
    }

    #endregion

    #region Constant Specs

    [Fact]
    public void Constants_ShouldMatchDocumentedValues()
    {
        // Arrange:
        // Act:
        // Assert:
        LocalizableConstant.Constraints.Property.MinLength.Should().Be(1);
        LocalizableConstant.Constraints.Property.MaxLength.Should().Be(128);
        LocalizableConstant.Constraints.Locale.MinLength.Should().Be(2);
        LocalizableConstant.Constraints.Locale.MaxLength.Should().Be(35);
        LocalizableConstant.Constraints.Value.MaxLength.Should().Be(4000);
        LocalizableConstant.Constraints.Collection.MaxTranslations.Should().Be(200);
        LocalizableConstant.Defaults.Locale.Should().Be("en");
        LocalizableConstant.Patterns.Locale.Should().Be(@"^[A-Za-z]{2,8}(?:-[A-Za-z0-9]{2,8})*$");
    }

    #endregion
}
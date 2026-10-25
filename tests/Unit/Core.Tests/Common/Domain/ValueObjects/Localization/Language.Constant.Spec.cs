using Core.Common.Domain.ValueObjects.Localization;

namespace Core.Tests.Common.Domain.ValueObjects.Localization;

public class LanguageConstantSpec
{
    [Fact]
    public void CodeMinLength_ShouldBeOne()
    {
        LanguageConstant.Constraints.Code.MinLength.Should().Be(1);
    }

    [Fact]
    public void CodeMaxLength_ShouldBeEight()
    {
        LanguageConstant.Constraints.Code.MaxLength.Should().Be(8);
    }

    [Fact]
    public void Default_ShouldBeEn()
    {
        LanguageConstant.Defaults.Default.Should().Be("en");
    }

    [Fact]
    public void CodePattern_ShouldAcceptTwoOrThreeLowercaseLetters()
    {
        LanguageConstant.Patterns.Code.Should().Be(@"^[a-z]{2,3}$");
    }
}
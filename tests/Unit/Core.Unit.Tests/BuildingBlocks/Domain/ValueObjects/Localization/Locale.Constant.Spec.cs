using Core.BuildingBlocks.Domain.ValueObjects.Localization;

namespace Core.Unit.Tests.BuildingBlocks.Domain.ValueObjects.Localization;

public class LocaleConstantSpec
{
    [Fact]
    public void CodeMinLength_ShouldBeOne()
    {
        LocaleConstant.Constraints.Code.MinLength.Should().Be(1);
    }

    [Fact]
    public void CodeMaxLength_ShouldBeThirtyFive()
    {
        LocaleConstant.Constraints.Code.MaxLength.Should().Be(35);
    }

    [Fact]
    public void Default_ShouldBeEn()
    {
        LocaleConstant.Defaults.Default.Should().Be("en");
    }

    [Fact]
    public void CodePattern_ShouldMatchBcp47Syntax()
    {
        LocaleConstant.Patterns.Code.Should().Be(
            @"^[a-z]{2,3}(?:-[A-Za-z]{4})?(?:-(?:[A-Z]{2}|[0-9]{3}))?$");
    }
}
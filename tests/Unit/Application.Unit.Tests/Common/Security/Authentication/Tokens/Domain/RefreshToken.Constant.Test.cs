using Application.Common.Security.Authentication.Tokens.Domain;

namespace Application.Unit.Tests.Common.Security.Authentication.Tokens.Domain;

[Trait("Category", "Unit")]
[Trait("Module", "Security")]
[Trait("Feature", "Authentication")]
public sealed class RefreshTokenConstantTest
{
    [Fact(DisplayName = "Token hash constraints should bound the stored hash")]
    public void TokenHashConstraints_ShouldBoundTheHash()
    {
        RefreshTokenConstant.Constraints.TokenHash.MinLength.Should().Be(1);
        RefreshTokenConstant.Constraints.TokenHash.MaxLength.Should().Be(500);
    }

    [Fact(DisplayName = "User identifier constraints should bound the owner")]
    public void UserIdConstraints_ShouldBoundTheOwner()
    {
        RefreshTokenConstant.Constraints.UserId.MaxLength.Should().Be(450);
    }

    [Fact(DisplayName = "Revocation reasons should expose the documented values")]
    public void RevocationReasons_ShouldExposeDocumentedValues()
    {
        RefreshTokenRevocationReason[] reasons = Enum
            .GetValues<RefreshTokenRevocationReason>();

        reasons.Should().BeEquivalentTo(
        [
            RefreshTokenRevocationReason.Replaced,
            RefreshTokenRevocationReason.UserLogout,
            RefreshTokenRevocationReason.UserLogoutAll,
            RefreshTokenRevocationReason.ReuseDetected
        ]);
    }

    [Fact(DisplayName = "Revocation reasons should have unique numeric values")]
    public void RevocationReasons_ShouldHaveUniqueValues()
    {
        int[] values = [.. Enum.GetValues<RefreshTokenRevocationReason>().Select(reason => (int)reason)];

        values.Should().OnlyHaveUniqueItems();
        values.Should().BeEquivalentTo([1, 2, 3, 4]);
    }

    [Fact(DisplayName = "Revocation reasons should all be defined")]
    public void RevocationReasons_ShouldAllBeDefined()
    {
        foreach (RefreshTokenRevocationReason reason in Enum.GetValues<RefreshTokenRevocationReason>())
        {
            Enum.IsDefined(reason).Should().BeTrue();
        }
    }
}

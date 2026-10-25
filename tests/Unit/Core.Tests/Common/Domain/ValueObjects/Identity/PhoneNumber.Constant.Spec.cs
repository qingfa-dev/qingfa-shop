using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Tests.Common.Domain.ValueObjects.Identity;

public class PhoneNumberConstantSpec
{
    [Fact]
    public void Constraints_ShouldDefineMaxLength()
    {
        // Arrange, Act & Assert
        PhoneNumberConstant.Constraints.Value.MaxLength.Should().Be(20);
    }

    [Fact]
    public void Patterns_ShouldDefineNumber()
    {
        // Arrange, Act & Assert
        PhoneNumberConstant.Patterns.Number.Should().Be(@"^\+?[0-9 ()\-]{5,20}$");
    }
}
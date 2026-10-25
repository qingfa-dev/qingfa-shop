using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Tests.Common.Domain.ValueObjects.Identity;

public class EmailAddressConstantSpec
{
    [Fact]
    public void Constraints_ShouldDefineLengths()
    {
        // Arrange, Act & Assert
        EmailAddressConstant.Constraints.Value.MinLength.Should().Be(1);
        EmailAddressConstant.Constraints.Value.MaxLength.Should().Be(254);
    }

    [Fact]
    public void Patterns_ShouldDefineAddress()
    {
        // Arrange, Act & Assert
        EmailAddressConstant.Patterns.Address.Should().Be(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$");
    }
}
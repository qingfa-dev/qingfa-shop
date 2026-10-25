using Core.Common.Domain.ValueObjects.Identity;

namespace Core.Tests.Common.Domain.ValueObjects.Identity;

public class UrlConstantSpec
{
    [Fact]
    public void Constraints_ShouldDefineLengths()
    {
        // Arrange, Act & Assert
        UrlConstant.Constraints.Value.MinLength.Should().Be(1);
        UrlConstant.Constraints.Value.MaxLength.Should().Be(2048);
    }
}
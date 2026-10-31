using Application.BuildingBlocks.Security.Authentication.Contexts;

namespace Application.Unit.Tests.BuildingBlocks.Security.Authentication.Contexts;

[Trait("Category", "Unit")]
public sealed class UserContextExtensionTest
{
    [Fact(DisplayName = "Actor should return UserName when available")]
    public void Actor_ShouldReturnUserName_WhenAvailable()
    {
        // Arrange
        var context = new Mock<IUserContext>();
        context.Setup(c => c.UserName).Returns("johndoe");
        context.Setup(c => c.UserId).Returns("123");

        // Act
        string? actor = context.Object.Actor();

        // Assert
        actor.Should().Be("johndoe");
    }

    [Fact(DisplayName = "Actor should return UserId when UserName is null")]
    public void Actor_ShouldReturnUserId_WhenUserNameIsNull()
    {
        // Arrange
        var context = new Mock<IUserContext>();
        context.Setup(c => c.UserName).Returns((string?)null);
        context.Setup(c => c.UserId).Returns("42");

        // Act
        string? actor = context.Object.Actor();

        // Assert
        actor.Should().Be("42");
    }

    [Fact(DisplayName = "Actor should return null when both UserName and UserId are null")]
    public void Actor_ShouldBeNull_WhenBothNull()
    {
        // Arrange
        var context = new Mock<IUserContext>();
        context.Setup(c => c.UserName).Returns((string?)null);
        context.Setup(c => c.UserId).Returns((string?)null);

        // Act
        string? actor = context.Object.Actor();

        // Assert
        actor.Should().BeNull();
    }
}

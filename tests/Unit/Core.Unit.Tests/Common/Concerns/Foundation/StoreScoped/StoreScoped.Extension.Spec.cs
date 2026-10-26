using Core.Common.Concerns.Foundation.StoreScoped;

namespace Core.UnitTests.Common.Concerns.Foundation.StoreScoped;

/// <summary>Specifications for <see cref="StoreScopedExtensions"/> and <see cref="StoreScopedValidator"/>.</summary>
public class StoreScopedExtensionSpec
{
    #region Helper Methods

    private sealed class TestStoreScoped : IStoreScoped<Guid>
    {
        public Guid StoreId { get; set; }
    }

    private sealed class TestStringStoreScoped : IStoreScoped<string>
    {
        public string StoreId { get; set; } = string.Empty;
    }

    private sealed class TestIntStoreScoped : IStoreScoped<int>
    {
        public int StoreId { get; set; }
    }

    /// <summary>Creates a successful result wrapping the given entity.</summary>
    /// <param name="entity">The entity to wrap.</param>
    /// <returns>A successful result containing the entity.</returns>
    private static Result<TestStoreScoped> Success(TestStoreScoped entity)
    {
        return Result<TestStoreScoped>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void EnsureStoreScoped_WithStoreId_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStoreScoped { StoreId = Guid.NewGuid() };

        // Act:
        var result = Success(entity).EnsureStoreScoped<TestStoreScoped, Guid>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureStoreScoped_DefaultStoreId_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStoreScoped();

        // Act:
        var result = Success(entity).EnsureStoreScoped<TestStoreScoped, Guid>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreScope_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<
            TestStoreScoped,
            Guid>(null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.Entity.Required");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateStoreScope_GuidEmpty_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStoreScoped { StoreId = Guid.Empty };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestStoreScoped, Guid>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreScope_ValidStoreId_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStoreScoped { StoreId = Guid.NewGuid() };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestStoreScoped, Guid>(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateStoreId_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestStoreScoped, Guid>(null!, Guid.NewGuid());

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.Entity.Required");
    }

    [Fact]
    public void ValidateStoreId_GuidEmpty_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStoreScoped { StoreId = Guid.NewGuid() };

        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestStoreScoped, Guid>(entity, Guid.Empty);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreId_ValidStoreId_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStoreScoped();
        var storeId = Guid.NewGuid();

        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestStoreScoped, Guid>(entity, storeId);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void SetStoreId_ValidStoreId_ShouldSetStoreId()
    {
        // Arrange:
        var entity = new TestStoreScoped();
        var storeId = Guid.NewGuid();

        // Act:
        var result = Success(entity).SetStoreId<TestStoreScoped, Guid>(storeId);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.StoreId.Should().Be(storeId);
    }

    [Fact]
    public void SetStoreId_GuidEmpty_ShouldFailWithoutMutation()
    {
        // Arrange:
        var keep = Guid.NewGuid();
        var entity = new TestStoreScoped { StoreId = keep };

        // Act:
        var result = Success(entity).SetStoreId<TestStoreScoped, Guid>(Guid.Empty);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
        entity.StoreId.Should().Be(keep);
    }

    [Fact]
    public void RequireStoreScoped_ValidStoreId_ShouldReturnEntity()
    {
        // Arrange:
        var entity = new TestStoreScoped { StoreId = Guid.NewGuid() };

        // Act:
        var value = Success(entity).RequireStoreScoped<TestStoreScoped, Guid>();

        // Assert:
        value.Should().BeSameAs(entity);
    }

    [Fact]
    public void RequireStoreScoped_GuidEmpty_ShouldThrow()
    {
        // Arrange:
        var entity = new TestStoreScoped();

        // Act:
        var act = () => Success(entity).RequireStoreScoped<TestStoreScoped, Guid>();

        // Assert:
        act.Should().Throw<ResultException>();
    }

    [Fact]
    public void EnsureStoreScoped_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestStoreScoped>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.EnsureStoreScoped<TestStoreScoped, Guid>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void EnsureStoreScoped_BlankString_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "   " };

        // Act:
        var result = Result<TestStringStoreScoped>.Success(entity).EnsureStoreScoped<TestStringStoreScoped, string>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void EnsureStoreScoped_ValidString_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "store-1" };

        // Act:
        var result = Result<TestStringStoreScoped>.Success(entity).EnsureStoreScoped<TestStringStoreScoped, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void SetStoreId_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestStoreScoped>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.SetStoreId<TestStoreScoped, Guid>(Guid.NewGuid());

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void SetStoreId_NullString_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "keep-me" };

        // Act:
        var result = Result<TestStringStoreScoped>.Success(entity).SetStoreId<TestStringStoreScoped, string>(null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
        entity.StoreId.Should().Be("keep-me");
    }

    [Fact]
    public void SetStoreId_BlankString_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "keep-me" };

        // Act:
        var result = Result<TestStringStoreScoped>.Success(entity).SetStoreId<TestStringStoreScoped, string>("   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
        entity.StoreId.Should().Be("keep-me");
    }

    [Fact]
    public void SetStoreId_ValidString_ShouldSetStoreId()
    {
        // Arrange:
        var entity = new TestStringStoreScoped();

        // Act:
        var result = Result<TestStringStoreScoped>.Success(entity).SetStoreId<TestStringStoreScoped, string>("store-1");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.StoreId.Should().Be("store-1");
    }

    [Fact]
    public void ValidateStoreScope_BlankString_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "   " };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestStringStoreScoped, string>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreScope_EmptyString_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = string.Empty };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestStringStoreScoped, string>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreScope_NullString_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = null! };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestStringStoreScoped, string>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreScope_ValidString_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "store-1" };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestStringStoreScoped, string>(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateStoreScope_DefaultInt_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestIntStoreScoped { StoreId = 0 };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestIntStoreScoped, int>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreScope_ValidInt_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestIntStoreScoped { StoreId = 42 };

        // Act:
        var result = StoreScopedValidator.ValidateStoreScope<TestIntStoreScoped, int>(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateStoreId_NullString_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "store-1" };

        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestStringStoreScoped, string>(entity, null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreId_BlankString_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestStringStoreScoped { StoreId = "store-1" };

        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestStringStoreScoped, string>(entity, "   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreId_ValidString_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStringStoreScoped();

        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestStringStoreScoped, string>(entity, "store-1");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateStoreId_DefaultInt_ShouldFailStoreIdRequired()
    {
        // Arrange:
        var entity = new TestIntStoreScoped();

        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestIntStoreScoped, int>(entity, 0);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("StoreScoped.StoreId.Required");
    }

    [Fact]
    public void ValidateStoreId_ValidInt_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestIntStoreScoped();

        // Act:
        var result = StoreScopedValidator.ValidateStoreId<TestIntStoreScoped, int>(entity, 42);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void RequireStoreScoped_InputFailure_ShouldThrow()
    {
        // Arrange:
        var input = Result<TestStoreScoped>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        // Assert:
        FluentActions.Invoking(() => input.RequireStoreScoped<TestStoreScoped, Guid>()).Should().Throw<ResultException>();
    }

    [Fact]
    public void Constants_ShouldMatchNestedDefinitions()
    {
        // Arrange:
        // Act:
        // Assert:
        StoreScopedConstant.Constraints.Store.AllowDefault.Should().BeFalse();
        StoreScopedConstant.Defaults.EmptyStoreId.Should().Be(Guid.Empty);
    }

    #endregion
}
using Core.Common.Concerns.Foundation.Tenantable;

namespace Core.UnitTests.Common.Concerns.Foundation.Tenantable;

/// <summary>Specifications for <see cref="TenantableExtensions"/> and <see cref="TenantableValidator"/>.</summary>
public class TenantableExtensionSpec
{
    #region Helper Methods

    private sealed class TestTenantable : ITenantable<Guid>
    {
        public Guid TenantId { get; set; }
    }

    private sealed class TestStringTenantable : ITenantable<string>
    {
        public string TenantId { get; set; } = string.Empty;
    }

    private sealed class TestIntTenantable : ITenantable<int>
    {
        public int TenantId { get; set; }
    }

    /// <summary>Creates a successful result wrapping the given entity.</summary>
    /// <param name="entity">The entity to wrap.</param>
    /// <returns>A successful result containing the entity.</returns>
    private static Result<TestTenantable> Success(TestTenantable entity)
    {
        return Result<TestTenantable>.Success(entity);
    }

    #endregion

    #region Test Cases

    [Fact]
    public void EnsureTenanted_WithTenantId_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestTenantable { TenantId = Guid.NewGuid() };

        // Act:
        var result = Success(entity).EnsureTenanted<TestTenantable, Guid>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureTenanted_DefaultTenantId_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestTenantable();

        // Act:
        var result = Success(entity).EnsureTenanted<TestTenantable, Guid>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenancy_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = TenantableValidator.ValidateTenancy<TestTenantable, Guid>(
            null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.Entity.Required");
    }

    #endregion

    #region Validator Specs

    [Fact]
    public void ValidateTenancy_GuidEmpty_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestTenantable { TenantId = Guid.Empty };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestTenantable, Guid>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenancy_ValidTenantId_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestTenantable { TenantId = Guid.NewGuid() };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestTenantable, Guid>(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateTenantId_NullEntity_ShouldFailEntityRequired()
    {
        // Arrange:
        // Act:
        var result = TenantableValidator.ValidateTenantId<TestTenantable, Guid>(null!, Guid.NewGuid());

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.Entity.Required");
    }

    [Fact]
    public void ValidateTenantId_GuidEmpty_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestTenantable { TenantId = Guid.NewGuid() };

        // Act:
        var result = TenantableValidator.ValidateTenantId<TestTenantable, Guid>(entity, Guid.Empty);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenantId_ValidTenantId_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestTenantable();
        var tenantId = Guid.NewGuid();

        // Act:
        var result = TenantableValidator.ValidateTenantId<TestTenantable, Guid>(entity, tenantId);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void SetTenantId_ValidTenantId_ShouldSetTenantId()
    {
        // Arrange:
        var entity = new TestTenantable();
        var tenantId = Guid.NewGuid();

        // Act:
        var result = Success(entity).SetTenantId<TestTenantable, Guid>(tenantId);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void SetTenantId_GuidEmpty_ShouldFailWithoutMutation()
    {
        // Arrange:
        var keep = Guid.NewGuid();
        var entity = new TestTenantable { TenantId = keep };

        // Act:
        var result = Success(entity).SetTenantId<TestTenantable, Guid>(Guid.Empty);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
        entity.TenantId.Should().Be(keep);
    }

    [Fact]
    public void RequireTenanted_ValidTenantId_ShouldReturnEntity()
    {
        // Arrange:
        var entity = new TestTenantable { TenantId = Guid.NewGuid() };

        // Act:
        var value = Success(entity).RequireTenanted<TestTenantable, Guid>();

        // Assert:
        value.Should().BeSameAs(entity);
    }

    [Fact]
    public void RequireTenanted_GuidEmpty_ShouldThrow()
    {
        // Arrange:
        var entity = new TestTenantable();

        // Act:
        var act = () => Success(entity).RequireTenanted<TestTenantable, Guid>();

        // Assert:
        act.Should().Throw<ResultException>();
    }

    [Fact]
    public void EnsureTenanted_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestTenantable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.EnsureTenanted<TestTenantable, Guid>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void EnsureTenanted_BlankString_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "   " };

        // Act:
        var result = Result<TestStringTenantable>.Success(entity).EnsureTenanted<TestStringTenantable, string>();

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void EnsureTenanted_ValidString_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "tenant-1" };

        // Act:
        var result = Result<TestStringTenantable>.Success(entity).EnsureTenanted<TestStringTenantable, string>();

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void SetTenantId_InputFailure_ShouldPropagateErrors()
    {
        // Arrange:
        var input = Result<TestTenantable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        var result = input.SetTenantId<TestTenantable, Guid>(Guid.NewGuid());

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Test.Fail");
    }

    [Fact]
    public void SetTenantId_NullString_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "keep-me" };

        // Act:
        var result = Result<TestStringTenantable>.Success(entity).SetTenantId<TestStringTenantable, string>(null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
        entity.TenantId.Should().Be("keep-me");
    }

    [Fact]
    public void SetTenantId_BlankString_ShouldFailWithoutMutation()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "keep-me" };

        // Act:
        var result = Result<TestStringTenantable>.Success(entity).SetTenantId<TestStringTenantable, string>("   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
        entity.TenantId.Should().Be("keep-me");
    }

    [Fact]
    public void SetTenantId_ValidString_ShouldSetTenantId()
    {
        // Arrange:
        var entity = new TestStringTenantable();

        // Act:
        var result = Result<TestStringTenantable>.Success(entity).SetTenantId<TestStringTenantable, string>("tenant-1");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        entity.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void ValidateTenancy_BlankString_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "   " };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestStringTenantable, string>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenancy_EmptyString_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = string.Empty };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestStringTenantable, string>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenancy_NullString_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = null! };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestStringTenantable, string>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenancy_ValidString_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "tenant-1" };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestStringTenantable, string>(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateTenancy_DefaultInt_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestIntTenantable { TenantId = 0 };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestIntTenantable, int>(entity);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenancy_ValidInt_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestIntTenantable { TenantId = 42 };

        // Act:
        var result = TenantableValidator.ValidateTenancy<TestIntTenantable, int>(entity);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateTenantId_NullString_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "tenant-1" };

        // Act:
        var result = TenantableValidator.ValidateTenantId<TestStringTenantable, string>(entity, null!);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenantId_BlankString_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestStringTenantable { TenantId = "tenant-1" };

        // Act:
        var result = TenantableValidator.ValidateTenantId<TestStringTenantable, string>(entity, "   ");

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenantId_ValidString_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestStringTenantable();

        // Act:
        var result = TenantableValidator.ValidateTenantId<TestStringTenantable, string>(entity, "tenant-1");

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateTenantId_DefaultInt_ShouldFailTenantIdRequired()
    {
        // Arrange:
        var entity = new TestIntTenantable();

        // Act:
        var result = TenantableValidator.ValidateTenantId<TestIntTenantable, int>(entity, 0);

        // Assert:
        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("Tenantable.TenantId.Required");
    }

    [Fact]
    public void ValidateTenantId_ValidInt_ShouldSucceed()
    {
        // Arrange:
        var entity = new TestIntTenantable();

        // Act:
        var result = TenantableValidator.ValidateTenantId<TestIntTenantable, int>(entity, 42);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void RequireTenanted_InputFailure_ShouldThrow()
    {
        // Arrange:
        var input = Result<TestTenantable>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        // Act:
        // Assert:
        FluentActions.Invoking(() => input.RequireTenanted<TestTenantable, Guid>()).Should().Throw<ResultException>();
    }

    [Fact]
    public void Constants_ShouldMatchNestedDefinitions()
    {
        // Arrange:
        // Act:
        // Assert:
        TenantableConstant.Constraints.Tenant.AllowDefault.Should().BeFalse();
        TenantableConstant.Defaults.EmptyTenantId.Should().Be(Guid.Empty);
    }

    #endregion
}
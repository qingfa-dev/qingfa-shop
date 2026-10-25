using Core.Common.Concerns.Foundation.Identifiable;

namespace Core.Tests.Common.Concerns.Foundation.Identifiable;

/// <summary>
/// Specifications for <see cref="IdentifiableExtensions"/> and
/// <see cref="IdentifiableValidator"/>.
/// </summary>
public sealed class IdentifiableExtensionSpec
{
    private const string IdRequired = "Identifiable.Id.Required";
    private const string EntityRequired = "Identifiable.Entity.Required";

    private sealed class GuidEntity : IIdentifiable<Guid>
    {
        public Guid Id { get; set; }
    }

    private sealed class StringEntity : IIdentifiable<string>
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class IntEntity : IIdentifiable<int>
    {
        public int Id { get; set; }
    }

    private static Result<GuidEntity> Success(GuidEntity entity) =>
        Result<GuidEntity>.Success(entity);

    private static void ShouldFailWith(
        Result result,
        string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeNull();
        result.Errors![0].Code.Should().Be(expectedCode);
    }

    // EnsureIdentified

    [Fact]
    public void EnsureIdentified_ValidGuid_ShouldPreserveEntity()
    {
        var entity = new GuidEntity { Id = Guid.NewGuid() };

        var result = Success(entity)
            .EnsureIdentified<GuidEntity, Guid>();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureIdentified_DefaultGuid_ShouldFail()
    {
        var result = Success(new GuidEntity())
            .EnsureIdentified<GuidEntity, Guid>();

        ShouldFailWith(result, IdRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureIdentified_InvalidStringId_ShouldFail(string? id)
    {
        var entity = new StringEntity { Id = id! };

        var result = Result<StringEntity>.Success(entity)
            .EnsureIdentified<StringEntity, string>();

        ShouldFailWith(result, IdRequired);
    }

    [Fact]
    public void EnsureIdentified_ValidInt_ShouldPreserveEntity()
    {
        var entity = new IntEntity { Id = 7 };

        var result = Result<IntEntity>.Success(entity)
            .EnsureIdentified<IntEntity, int>();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void EnsureIdentified_InputFailure_ShouldPropagateError()
    {
        var input = Result<GuidEntity>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        var result = input.EnsureIdentified<GuidEntity, Guid>();

        ShouldFailWith(result, "Test.Fail");
    }

    // RequireIdentified

    [Fact]
    public void RequireIdentified_ValidGuid_ShouldReturnEntity()
    {
        var entity = new GuidEntity { Id = Guid.NewGuid() };

        var value = Success(entity)
            .RequireIdentified<GuidEntity, Guid>();

        value.Should().BeSameAs(entity);
    }

    [Fact]
    public void RequireIdentified_DefaultGuid_ShouldThrow()
    {
        var entity = new GuidEntity();

        FluentActions.Invoking(() =>
                Success(entity).RequireIdentified<GuidEntity, Guid>())
            .Should()
            .Throw<ResultException>();
    }

    [Fact]
    public void RequireIdentified_BlankStringId_ShouldThrow()
    {
        var entity = new StringEntity { Id = "   " };

        FluentActions.Invoking(() =>
                Result<StringEntity>.Success(entity)
                    .RequireIdentified<StringEntity, string>())
            .Should()
            .Throw<ResultException>();
    }

    [Fact]
    public void RequireIdentified_InputFailure_ShouldThrow()
    {
        var input = Result<GuidEntity>.Fail(
            Error.UnprocessableEntity("Test.Fail", "boom"));

        FluentActions.Invoking(() =>
                input.RequireIdentified<GuidEntity, Guid>())
            .Should()
            .Throw<ResultException>();
    }

    [Fact]
    public void RequireIdentified_ValidInt_ShouldReturnEntity()
    {
        var entity = new IntEntity { Id = 7 };

        var value = Result<IntEntity>.Success(entity)
            .RequireIdentified<IntEntity, int>();

        value.Should().BeSameAs(entity);
    }

    // ValidateIdentification

    [Fact]
    public void ValidateIdentification_NullEntity_ShouldFail()
    {
        var result =
            IdentifiableValidator.ValidateIdentification<GuidEntity, Guid>(
                null!);

        ShouldFailWith(result, EntityRequired);
    }

    [Fact]
    public void ValidateIdentification_ValidGuid_ShouldPreserveEntity()
    {
        var entity = new GuidEntity { Id = Guid.NewGuid() };

        var result =
            IdentifiableValidator.ValidateIdentification<GuidEntity, Guid>(
                entity);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateIdentification_DefaultGuid_ShouldFail()
    {
        var result =
            IdentifiableValidator.ValidateIdentification<GuidEntity, Guid>(
                new GuidEntity());

        ShouldFailWith(result, IdRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateIdentification_InvalidStringId_ShouldFail(
        string? id)
    {
        var entity = new StringEntity { Id = id! };

        var result =
            IdentifiableValidator.ValidateIdentification<StringEntity, string>(
                entity);

        ShouldFailWith(result, IdRequired);
    }

    [Fact]
    public void ValidateIdentification_ValidStringId_ShouldPreserveEntity()
    {
        var entity = new StringEntity { Id = "entity-1" };

        var result =
            IdentifiableValidator.ValidateIdentification<StringEntity, string>(
                entity);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    [Fact]
    public void ValidateIdentification_DefaultInt_ShouldFail()
    {
        var result =
            IdentifiableValidator.ValidateIdentification<IntEntity, int>(
                new IntEntity());

        ShouldFailWith(result, IdRequired);
    }

    [Fact]
    public void ValidateIdentification_ValidInt_ShouldPreserveEntity()
    {
        var entity = new IntEntity { Id = 42 };

        var result =
            IdentifiableValidator.ValidateIdentification<IntEntity, int>(
                entity);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(entity);
    }

    // ValidateId

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateId_InvalidString_ShouldFail(string? id)
    {
        var result = IdentifiableValidator.ValidateId<string>(id!);

        ShouldFailWith(result, IdRequired);
    }

    [Fact]
    public void ValidateId_ValidString_ShouldReturnValue()
    {
        const string id = "entity-1";

        var result = IdentifiableValidator.ValidateId(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
    }

    [Fact]
    public void ValidateId_EmptyGuid_ShouldFail()
    {
        var result = IdentifiableValidator.ValidateId(Guid.Empty);

        ShouldFailWith(result, IdRequired);
    }

    [Fact]
    public void ValidateId_ValidGuid_ShouldReturnValue()
    {
        var id = Guid.NewGuid();

        var result = IdentifiableValidator.ValidateId(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
    }

    [Fact]
    public void ValidateId_DefaultInt_ShouldFail()
    {
        var result = IdentifiableValidator.ValidateId(0);

        ShouldFailWith(result, IdRequired);
    }

    [Fact]
    public void ValidateId_ValidInt_ShouldReturnValue()
    {
        const int id = 42;

        var result = IdentifiableValidator.ValidateId(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(id);
    }

    // Constants

    [Fact]
    public void Constants_ShouldExposeExpectedDefaults()
    {
        IdentifiableConstant.Constraints.Id.AllowDefault.Should().BeFalse();
        IdentifiableConstant.Defaults.EmptyId.Should().Be(Guid.Empty);
    }
}
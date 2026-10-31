using Infrastructure.BuildingBlocks.Adapters.OpenApi.Options;

namespace Infrastructure.Unit.Tests.BuildingBlocks.Adapters.OpenApi;

/// <summary>Unit tests for the OpenAPI schema reference naming rules.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Integration")]
public sealed class OpenApiSchemaNamingTest
{
    [Theory(DisplayName = "GetSchemaReferenceId should name scalar, nullable, array, and generic types")]
    [InlineData(typeof(string), "String")]
    [InlineData(typeof(int?), "Int32")]
    [InlineData(typeof(int[]), "ArrayOfInt32")]
    [InlineData(typeof(List<int>), "ListOfInt32")]
    [InlineData(typeof(Dictionary<string, int>), "DictionaryOfStringAndInt32")]
    [InlineData(typeof(List<List<int>>), "ListOfListOfInt32")]
    public void GetSchemaReferenceId_ShouldNameCommonShapes(Type type, string expected)
    {
        OpenApiSchemaNaming.GetSchemaReferenceId(type).Should().Be(expected);
    }

    [Fact(DisplayName = "GetSchemaReferenceId should prefix nested types with their declaring type")]
    public void GetSchemaReferenceId_WithNestedType_ShouldPrefixDeclaringType()
    {
        OpenApiSchemaNaming.GetSchemaReferenceId(typeof(Outer.Inner))
            .Should().Be("OpenApiSchemaNamingTestOuterInner");

        OpenApiSchemaNaming.GetSchemaReferenceId(typeof(Outer.GenericInner<int>))
            .Should().Be("OpenApiSchemaNamingTestOuterGenericInnerOfInt32");
    }

    [Fact(DisplayName = "GetSchemaReferenceId should name anonymous types")]
    public void GetSchemaReferenceId_WithAnonymousType_ShouldNameAnonymousShape()
    {
        Type anonymous = new { Name = string.Empty }.GetType();

        OpenApiSchemaNaming.GetSchemaReferenceId(anonymous)
            .Should().Be("AnonymousTypeOfString");
    }

    [Fact(DisplayName = "GetSchemaReferenceId should preserve generic parameter names")]
    public void GetSchemaReferenceId_WithGenericParameter_ShouldReturnParameterName()
    {
        Type parameter = typeof(List<>).GetGenericArguments()[0];

        OpenApiSchemaNaming.GetSchemaReferenceId(parameter).Should().Be("T");
    }

    private static class Outer
    {
        public sealed class Inner;

        public sealed class GenericInner<T>;
    }
}

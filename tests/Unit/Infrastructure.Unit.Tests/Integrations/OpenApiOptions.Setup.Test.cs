using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Infrastructure.Integrations.OpenApi.Options;

using Microsoft.AspNetCore.OpenApi;

namespace Infrastructure.Unit.Tests.Integrations;

/// <summary>Unit tests for the OpenAPI option setup.</summary>
[Trait("Category", "Unit")]
[Trait("Module", "Shared")]
[Trait("Feature", "Integration")]
public sealed class OpenApiOptionsSetupTest
{
    private sealed class SchemaProbe
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact(DisplayName = "ConfigureCustomOptions should return the same options and set the schema naming delegate")]
    public void ConfigureCustomOptions_ShouldConfigureOptions()
    {
        var options = new OpenApiOptions();

        OpenApiOptions result = options.ConfigureCustomOptions();

        result.Should().BeSameAs(options);
        options.CreateSchemaReferenceId.Should().NotBeNull();

        JsonTypeInfo probe = JsonSerializerOptions.Default.GetTypeInfo(typeof(SchemaProbe))!;
        options.CreateSchemaReferenceId!(probe).Should().Be("OpenApiOptionsSetupTestSchemaProbe");

        JsonTypeInfo text = JsonSerializerOptions.Default.GetTypeInfo(typeof(string))!;
        options.CreateSchemaReferenceId!(text).Should().BeNull();
    }

    [Fact(DisplayName = "ConfigureCustomOptions should reject null options")]
    public void ConfigureCustomOptions_WithNull_ShouldThrow()
    {
        OpenApiOptions? options = null;

        Action act = () => options!.ConfigureCustomOptions();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "OpenApi option constants should expose the documented values")]
    public void Constants_ShouldExposeDocumentedValues()
    {
        OpenApiOptionsConstant.Info.Title.Should().Be("QingFa Shop API");
        OpenApiOptionsConstant.Info.Version.Should().Be("v1");
        OpenApiOptionsConstant.Info.Endpoint.Should().Be("/api/scalar");
        OpenApiOptionsConstant.Info.Description.Should().NotBeNullOrWhiteSpace();
    }
}

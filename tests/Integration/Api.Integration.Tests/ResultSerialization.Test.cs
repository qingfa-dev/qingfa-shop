using System.Text.Json;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Infrastructure.Integrations.Serialization;

namespace Api.Integration.Tests;

[Trait("Category", "Unit")]
public sealed class ResultSerializationTest
{
    [Fact]
    public void Success_ShouldSerializeValueAndHideComputedMembers()
    {
        Result<int> result = Result<int>.Ok(42);

        string json = JsonSerializer.Serialize(result, ApiJson.Options);

        using JsonDocument document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("value").GetInt32().Should().Be(42);
        document.RootElement.TryGetProperty("isFailure", out _).Should().BeFalse();
        document.RootElement.TryGetProperty("hasValue", out _).Should().BeFalse();
    }

    [Fact]
    public void Failure_ShouldSerializeErrorsAndStringEnums()
    {
        Result result = Result.Fail(Error.NotFound("test.notfound", "missing"));

        string json = JsonSerializer.Serialize(result, ApiJson.Options);

        using JsonDocument document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        document.RootElement.TryGetProperty("value", out _).Should().BeFalse();
        document.RootElement.GetProperty("metadata").GetRawText().Should().Be("{}");

        JsonElement error = document.RootElement.GetProperty("errors")[0];
        error.GetProperty("severity").GetString().Should().Be("warning");
        error.GetProperty("type").GetString().Should().Be("https://errors.kernel/not-found");
    }
}

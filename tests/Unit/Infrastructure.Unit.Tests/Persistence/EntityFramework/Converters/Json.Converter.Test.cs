using System.Text.Json;

using Infrastructure.Persistence.EntityFramework.Converters;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.Converters;

/// <summary>
/// Round-trip tests for the JSON value converters. Object values must come back as
/// CLR primitives and dictionaries, never as <see cref="JsonElement"/> instances.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Module", "Infrastructure")]
[Trait("Feature", "Persistence")]
public sealed class JsonConverterTest
{
    [Fact(DisplayName = "Metadata should round-trip primitive values as CLR types")]
    public void Metadata_WhenRoundTripped_ShouldReturnClrPrimitives()
    {
        // Arrange
        var metadata = new DefaultMetadata
        {
            ["name"] = "value",
            ["enabled"] = true,
            ["count"] = 5,
            ["ratio"] = 1.5m
        };

        // Act
        string json = (string)MetadataValueConverter.Instance.ConvertToProvider(metadata)!;
        var result = (DefaultMetadata)MetadataValueConverter.Instance.ConvertFromProvider(json)!;

        // Assert
        result["name"].Should().BeOfType<string>().Which.Should().Be("value");
        result["enabled"].Should().BeOfType<bool>().Which.Should().BeTrue();
        result["count"].Should().NotBeOfType<JsonElement>();
        Convert.ToInt64(result["count"]).Should().Be(5);
        result["ratio"].Should().NotBeOfType<JsonElement>();
        Convert.ToDecimal(result["ratio"]).Should().Be(1.5m);
    }

    [Fact(DisplayName = "Metadata should round-trip nested objects as dictionaries")]
    public void Metadata_WhenNestedObject_ShouldReturnDictionary()
    {
        // Arrange
        var metadata = new DefaultMetadata
        {
            ["nested"] = new Dictionary<string, object>
            {
                ["inner"] = "value",
                ["number"] = 3
            }
        };

        // Act
        string json = (string)MetadataValueConverter.Instance.ConvertToProvider(metadata)!;
        var result = (DefaultMetadata)MetadataValueConverter.Instance.ConvertFromProvider(json)!;

        // Assert
        result["nested"].Should().BeAssignableTo<IDictionary<string, object>>();
        var nested = (IDictionary<string, object>)result["nested"]!;
        nested["inner"].Should().Be("value");
        Convert.ToInt64(nested["number"]).Should().Be(3);
    }

    [Fact(DisplayName = "Metadata should keep case-insensitive key lookup after a round trip")]
    public void Metadata_WhenRoundTripped_ShouldKeepCaseInsensitiveKeys()
    {
        // Arrange
        var metadata = new DefaultMetadata { ["SomeKey"] = "value" };

        // Act
        string json = (string)MetadataValueConverter.Instance.ConvertToProvider(metadata)!;
        var result = (DefaultMetadata)MetadataValueConverter.Instance.ConvertFromProvider(json)!;

        // Assert
        result["somekey"].Should().Be("value");
        result["SOMEKEY"].Should().Be("value");
    }

    [Fact(DisplayName = "Nullable metadata should keep null and read an empty object as empty metadata")]
    public void NullableMetadata_WhenNull_ShouldKeepNullAndReadEmptyObject()
    {
        // Act
        object? providerValue = NullableMetadataValueConverter.Instance.ConvertToProvider(null);
        var result = (DefaultMetadata)NullableMetadataValueConverter.Instance.ConvertFromProvider("{}")!;

        // Assert
        providerValue.Should().BeNull();
        result.Count.Should().Be(0);
    }

    [Fact(DisplayName = "Dictionary should round-trip values as CLR types")]
    public void Dictionary_WhenRoundTripped_ShouldReturnClrPrimitives()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["name"] = "value",
            ["enabled"] = false,
            ["count"] = 7,
            ["nested"] = new Dictionary<string, object> { ["inner"] = 1 }
        };

        // Act
        string json = (string)DictionaryJsonValueConverter.Instance.ConvertToProvider(payload)!;
        var result = (Dictionary<string, object>)DictionaryJsonValueConverter.Instance.ConvertFromProvider(json)!;

        // Assert
        result["name"].Should().Be("value");
        result["enabled"].Should().Be(false);
        result["count"].Should().NotBeOfType<JsonElement>();
        Convert.ToInt64(result["count"]).Should().Be(7);
        result["nested"].Should().BeAssignableTo<Dictionary<string, object>>();
    }

    [Fact(DisplayName = "Nullable dictionary should keep null and read an empty object as an empty dictionary")]
    public void NullableDictionary_WhenNull_ShouldKeepNullAndReadEmptyObject()
    {
        // Act
        object? providerValue = NullableDictionaryJsonValueConverter.Instance.ConvertToProvider(null);
        var result = (Dictionary<string, object>)NullableDictionaryJsonValueConverter.Instance.ConvertFromProvider("{}")!;

        // Assert
        providerValue.Should().BeNull();
        result.Count.Should().Be(0);
    }

    [Fact(DisplayName = "Dictionary should tolerate an empty provider value")]
    public void Dictionary_WhenProviderValueIsEmpty_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = (Dictionary<string, object>)
            DictionaryJsonValueConverter.Instance.ConvertFromProvider(string.Empty)!;

        // Assert
        result.Should().BeEmpty();
    }
}

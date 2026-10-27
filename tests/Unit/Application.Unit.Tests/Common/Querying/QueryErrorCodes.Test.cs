using Application.Common.Querying;

namespace Application.Tests.Common.Querying;

[Trait("Category", "Unit")]
public sealed class QueryErrorCodesTest
{
    [Fact(DisplayName = "Codes Should Be Unique")]
    public void Codes_ShouldBeUnique()
    {
        // Arrange
        var codes = typeof(QueryErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact(DisplayName = "Codes Should Follow Naming Convention")]
    public void Codes_ShouldFollowNamingConvention()
    {
        // Arrange
        var codes = typeof(QueryErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert
        codes.Should().AllSatisfy(c => c.Should().StartWith("Query."));
    }

    [Fact(DisplayName = "Codes Should Follow PascalCase Convention")]
    public void Codes_ShouldFollowPascalCaseConvention()
    {
        // Arrange
        var codes = typeof(QueryErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToList();

        // Assert - format: Query.{Category}.{Code} where Category and Code are PascalCase
        // (allowing multiple PascalCase words concatenated, e.g., "ValueInvalid", "TooManyConditions")
        foreach (var code in codes)
        {
            var segments = code!.Split('.');
            segments.Should().HaveCountGreaterThan(1, $"Code '{code}' should have at least two segments");
            segments[0].Should().Be("Query");

            for (int i = 1; i < segments.Length; i++)
            {
                // Each segment should be PascalCase (first letter uppercase, rest lowercase/digits)
                // allowing multiple PascalCase words concatenated like "ValueInvalid", "TooManyConditions"
                segments[i].Should().MatchRegex("^([A-Z][a-z0-9]*)+$",
                    $"Segment '{segments[i]}' in code '{code}' should be PascalCase (allowing concatenated words)");
            }
        }
    }

    [Fact(DisplayName = "Codes Should Match QueryOutcome Failure Properties")]
    public void Codes_ShouldMatchQueryOutcomeFailureProperties()
    {
        // Arrange
        var codes = typeof(QueryErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(c => c != null)
            .ToHashSet();

        var failureProperties = typeof(QueryOutcome.Failure)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(Error))
            .Select(m => m.Name)
            .ToList();

        // Assert - each failure property should have a corresponding code
        var outcomeCodes = codes.Where(c => c!.StartsWith("Query.") && !c.StartsWith("Query.Map.")).ToList();
        var codeSegments = outcomeCodes.Select(c => c!.Split('.').Last()).ToHashSet();
        
        // Known category prefixes that appear in property names but not in code segments
        var categoryPrefixes = new[] { "Search" };
        
        // All QueryOutcome.Failure properties should have codes
        foreach (var prop in failureProperties)
        {
            // Property names follow pattern: {Category}{Code} or {Prefix}{Category}{Code}
            // where Category might be Filter, Order, Page, Search, etc.
            // Code segments are just the Code part (e.g., "TooManyTerms" vs property "TooManySearchTerms")
            var normalizedProp = prop;
            foreach (var cat in categoryPrefixes)
            {
                // Remove category prefix from middle: "TooManySearchTerms" -> "TooManyTerms"
                if (normalizedProp.Contains(cat))
                {
                    normalizedProp = normalizedProp.Replace(cat, "");
                }
            }
            
            // Property names follow pattern: {Category}{Code} or {Prefix}{Category}{Code}
            // where Category might be Filter, Order, Page, Search, etc.
            // Code segments are just the Code part (e.g., "TooManyTerms" vs property "TooManySearchTerms")
            var hasMatch = codeSegments.Any(seg => 
                normalizedProp.EndsWith(seg) ||  // Direct suffix match on normalized
                (normalizedProp.Length > seg.Length && normalizedProp[^seg.Length..] == seg)); // Suffix match
            
            hasMatch.Should().BeTrue($"Code for {prop} should exist (expected segment like {string.Join(", ", codeSegments)})");
        }
    }
}
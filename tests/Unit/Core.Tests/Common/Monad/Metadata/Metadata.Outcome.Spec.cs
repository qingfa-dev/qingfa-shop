namespace Core.UnitTests.Common.Monad.Metadata;

/// <summary>
/// Tests for <see cref="MetadataOutcome"/> success and failure cases.
/// </summary>
/// <remarks>
/// Verifies that metadata outcome cases expose stable codes and meaningful messages,
/// and that failure codes follow the expected naming convention.
/// </remarks>
[Trait("Category", "Unit")]
public class MetadataOutcomeSpec
{
    #region Success

    [Fact]
    public void Success_ShouldBeDiscoverable()
    {
        // Arrange
        var success = typeof(MetadataOutcome.Success);

        // Act

        // Assert
        success.Should().NotBeNull();
    }

    #endregion

    #region Failure

    [Fact]
    public void Failure_ShouldExposeExpectedCases()
    {
        // Arrange
        var failure = typeof(MetadataOutcome.Failure);

        // Act
        var cases = CollectOutcomeTypes(failure).ToArray();

        // Assert
        cases.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    public void FailureCase_ShouldDefineValidCodeAndMessage(
        string typeName,
        string code,
        string message)
    {
        // Arrange

        // Act

        // Assert
        typeName.Should().NotBeNullOrWhiteSpace();
        code.Should().NotBeNullOrWhiteSpace();
        message.Should().NotBeNullOrWhiteSpace();

        code.Should().MatchRegex(
            @"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]+)+$");
    }

    public static TheoryData<string, string, string> FailureCases()
    {
        var data = new TheoryData<string, string, string>();

        foreach (var type in CollectOutcomeTypes(typeof(MetadataOutcome.Failure)))
        {
            data.Add(
                type.FullName!,
                GetConstant(type, "Code"),
                GetConstant(type, "Message"));
        }

        return data;
    }

    #endregion

    #region Helpers

    private static IEnumerable<Type> CollectOutcomeTypes(Type type)
    {
        foreach (var nested in type.GetNestedTypes())
        {
            if (HasConstant(nested, "Code"))
            {
                yield return nested;
                continue;
            }

            foreach (var deeper in CollectOutcomeTypes(nested))
            {
                yield return deeper;
            }
        }
    }

    private static bool HasConstant(Type type, string name)
        => type.GetField(
            name,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static) is not null;

    private static string GetConstant(Type type, string name)
        => (string)type.GetField(
            name,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

    #endregion
}
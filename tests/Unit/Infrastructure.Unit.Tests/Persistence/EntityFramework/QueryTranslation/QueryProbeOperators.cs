using System.Linq.Expressions;

using Infrastructure.Persistence.EntityFramework.QueryTranslation;

namespace Infrastructure.Unit.Tests.Persistence.EntityFramework.QueryTranslation;

/// <summary>Test operator registry with a registered custom operator for testing.</summary>
public static class QueryProbeOperators
{
    /// <summary>A custom operator name that is registered in the test registry.</summary>
    public const string Registered = "customEquals";

    /// <summary>A custom operator name that is NOT registered, used for negative testing.</summary>
    public const string Unregistered = "unregisteredOp";

    /// <summary>Creates an operator registry with the registered custom operator.</summary>
    public static EfOperatorRegistry Create()
    {
        return new EfOperatorRegistry()
            .Register(Registered, (member, value) => Expression.Equal(member, value));
    }
}
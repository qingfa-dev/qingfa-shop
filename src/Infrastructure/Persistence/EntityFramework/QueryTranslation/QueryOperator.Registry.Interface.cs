using System.Linq.Expressions;

namespace Infrastructure.Persistence.EntityFramework.QueryTranslation;

/// <summary>Provides custom operator implementations for filter translation.</summary>
/// <remarks>
/// A custom operator is named in the filter text and resolved by the adapter. An
/// operator that is not registered is reported as a failure, never thrown.
/// Implementations can register custom operators (for example full-text or
/// geo-spatial) by providing an expression builder for the operator name.
/// </remarks>
public interface IQueryOperatorRegistry
{
    /// <summary>Builds an expression for a custom operator.</summary>
    /// <param name="name">The custom operator name (without the '#' prefix).</param>
    /// <param name="member">The member expression to apply the operator to.</param>
    /// <param name="value">The operand expression to compare against.</param>
    /// <returns>The expression representing the custom operation, or a failure if unsupported.</returns>
    Result<Expression> Build(string name, Expression member, Expression value);

    /// <summary>Checks if a custom operator is registered.</summary>
    /// <param name="name">The custom operator name (without the '#' prefix).</param>
    /// <returns>True if the operator is registered.</returns>
    bool Contains(string name);
}
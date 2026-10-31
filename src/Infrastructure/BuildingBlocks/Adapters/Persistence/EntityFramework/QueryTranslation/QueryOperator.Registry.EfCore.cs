using System.Linq.Expressions;

using Application.BuildingBlocks.Querying;

namespace Infrastructure.BuildingBlocks.Adapters.Persistence.EntityFramework.QueryTranslation;

/// <summary>Registers named custom operators without teaching Core about EF.</summary>
/// <remarks>
/// Register every operator at startup, before the registry is shared; the lookup is
/// not synchronized against later registration.
/// </remarks>
public sealed class EfOperatorRegistry : IQueryOperatorRegistry
{
    private readonly Dictionary<string, Func<Expression, Expression, Expression>> _builders =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers a builder under a name, replacing any earlier one.</summary>
    /// <param name="name">The operator name, without the '#' prefix.</param>
    /// <param name="builder">Builds the expression from the member and the operand.</param>
    /// <returns>This registry, for chaining.</returns>
    public EfOperatorRegistry Register(string name, Func<Expression, Expression, Expression> builder)
    {
        // Validate: the name and builder must not be null.
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(builder);
        // Replace: later registration overwrites earlier one for the same name.
        _builders[name] = builder;
        return this;
    }

    /// <inheritdoc />
    public bool Contains(string name) =>
        // Validate: empty names are not registered.
        !string.IsNullOrWhiteSpace(name) && _builders.ContainsKey(name);

    /// <inheritdoc />
    /// <remarks>
    /// Catch: builder exceptions (InvalidOperationException, ArgumentException) are caught
    /// and reported as unsupported operators rather than crashes, because a builder that
    /// can't fit the member type is a configuration error, not a fault.
    /// </remarks>
    public Result<Expression> Build(string name, Expression member, Expression value)
    {
        // Lookup: find the registered builder for the operator name.
        if (string.IsNullOrWhiteSpace(name) ||
            !_builders.TryGetValue(name, out Func<Expression, Expression, Expression>? builder))
        {
            return Result<Expression>.Fail(
                QueryOutcome.Failure.OperatorUnsupported(name ?? string.Empty));
        }

        try
        {
            return Result<Expression>.Success(builder(member, value));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Catch: a builder that cannot fit the member type is an unsupported use, not a crash.
            return Result<Expression>.Fail(QueryOutcome.Failure.OperatorUnsupported(name));
        }
    }
}
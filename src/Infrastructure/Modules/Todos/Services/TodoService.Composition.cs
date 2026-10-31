using Application.Modules.Todos.Abstractions;

using Infrastructure.Modules.Todos.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Modules.Todos.Services;

/// <summary>Registers cross-aggregate Todo services.</summary>
public static class TodoServiceComposition
{
    /// <summary>Adds the Todo reference cleaner.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static WebApplicationBuilder AddTodoServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddScoped<ITodoReferenceCleaner, TodoReferenceCleaner>();
        return builder;
    }
}

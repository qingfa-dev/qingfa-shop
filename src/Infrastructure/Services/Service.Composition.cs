using Application.Features.Todos.Abstractions;

using Infrastructure.Services.Todos;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Services;

public static class ServiceComposition
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Create: Cross-aggregate reference cleanup used by the delete use cases.
        services.TryAddScoped<ITodoReferenceCleaner, TodoReferenceCleaner>();
        return services;
    }
}
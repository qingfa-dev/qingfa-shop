using Carter;

using Application;

using Infrastructure;

using Microsoft.Extensions.DependencyInjection.Extensions;

using ServiceDefaults;

using Web.Api.Endpoints.Authorization;
using Web.Api.Endpoints.Results;
using Web.Api.Endpoints.Todos;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register: Service defaults (OpenTelemetry, health checks, resilience, service discovery).
builder.AddServiceDefaults();

// Register: Application layer services (MediatR, validators, Mapster).
builder.AddApplication();

// Register: Infrastructure layer services (EF Core, caching, repositories, query maps).
builder.AddInfrastructure();

// Register: Carter endpoint modules (the permission catalog and checks).
builder.Services.AddCarter(configurator: configurator =>
{
    configurator.WithModule<PermissionRegistryEndpoint>();
    configurator.WithModule<TodoEndpoint>();
    configurator.WithModule<TodoListEndpoint>();
    configurator.WithModule<TodoCategoryEndpoint>();
    configurator.WithModule<TodoReminderEndpoint>();
});

// Initialize: production uses the system clock; TryAdd lets a test host replace it
// with FakeTimeProvider without touching this composition.
builder.Services.TryAddSingleton(TimeProvider.System);

// Initialize: validation failures are client errors. The default handler covers
// everything else as a 500 problem response.
builder.Services.AddProblemDetails();

// Initialize: one serializer governs request binding and response writing, so results
// and endpoint payloads share naming, enum, and null-handling rules.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = ApiJson.Options.PropertyNamingPolicy;
    options.SerializerOptions.PropertyNameCaseInsensitive = ApiJson.Options.PropertyNameCaseInsensitive;
    options.SerializerOptions.DefaultIgnoreCondition = ApiJson.Options.DefaultIgnoreCondition;
    options.SerializerOptions.WriteIndented = ApiJson.Options.WriteIndented;
    options.SerializerOptions.Converters.Clear();
    foreach (var converter in ApiJson.Options.Converters)
        options.SerializerOptions.Converters.Add(converter);
});

WebApplication app = builder.Build();

// Initialize: warm application services and run the persistence pipeline when configured.
app.UseInfrastructure();

app.UseExceptionHandler();

app.MapCarter();
app.MapDefaultEndpoints();

// Handle: /time exposes the injected TimeProvider so integration tests can prove the
// fake clock is wired through DI rather than the endpoint reading the system clock.
app.MapGet("/time", (TimeProvider clock) => Results.Json(new { utcNow = clock.GetUtcNow() }));

app.Run();
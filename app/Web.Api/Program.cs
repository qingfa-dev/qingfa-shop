using System.Text.Json;

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
// and endpoint payloads share naming, enum, escaping, and null-handling rules.
// Enforce: JsonOptions.SerializerOptions is read-only, so mirror every setting that
// changes the wire format; a partial copy would let escaping or numbers drift.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    JsonSerializerOptions target = options.SerializerOptions;
    JsonSerializerOptions source = ApiJson.Options;

    target.AllowTrailingCommas = source.AllowTrailingCommas;
    target.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
    target.DictionaryKeyPolicy = source.DictionaryKeyPolicy;
    target.Encoder = source.Encoder;
    target.IgnoreReadOnlyFields = source.IgnoreReadOnlyFields;
    target.IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties;
    target.IncludeFields = source.IncludeFields;
    target.MaxDepth = source.MaxDepth;
    target.NumberHandling = source.NumberHandling;
    target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
    target.PropertyNamingPolicy = source.PropertyNamingPolicy;
    target.ReadCommentHandling = source.ReadCommentHandling;
    target.ReferenceHandler = source.ReferenceHandler;
    target.UnmappedMemberHandling = source.UnmappedMemberHandling;
    target.WriteIndented = source.WriteIndented;

    target.Converters.Clear();
    foreach (var converter in source.Converters)
        target.Converters.Add(converter);
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
using System.Text.Json;

using Carter;

using Application;

using Core.Common.Monad.Errors;
using Core.Common.Monad.Results;

using Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ServiceDefaults;

using Web.Api.Endpoints.Authorization;
using Infrastructure.Integrations.Serialization;
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

// Initialize: one serializer governs request binding and response writing, so
// results and endpoint payloads share naming, enum, escaping, and null rules.
builder.AddApiJsonOptions();

// Initialize: authentication failures keep the Result contract instead of writing
// an empty body or ProblemDetails that clients would have to special-case.
builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await Result.Fail(Error.Unauthorized(
                        "Authentication.Required",
                        "Authentication is required."))
                    .ToApiResult()
                    .ExecuteAsync(context.HttpContext);
            },
            OnForbidden = context => Result.Fail(Error.Forbidden(
                    "Authorization.Forbidden",
                    "The caller is not allowed to perform this operation."))
                .ToApiResult()
                .ExecuteAsync(context.HttpContext),
        };
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
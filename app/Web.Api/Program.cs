using Application;

using Infrastructure;

using Microsoft.Extensions.DependencyInjection.Extensions;

using ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register: Service defaults (OpenTelemetry, health checks, resilience, service discovery).
builder.AddServiceDefaults();

// Register: Application layer services (MediatR, validators, Mapster).
builder.AddApplication();

// Register: Infrastructure layer services (EF Core, caching, repositories, query maps).
builder.AddInfrastructure();

// Initialize: production uses the system clock; TryAdd lets a test host replace it
// with FakeTimeProvider without touching this composition.
builder.Services.TryAddSingleton(TimeProvider.System);

// Initialize: validation failures are client errors. The default handler covers
// everything else as a 500 problem response.
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

// Initialize: warm application services and run the persistence pipeline when configured.
app.UseInfrastructure();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

// Handle: /time exposes the injected TimeProvider so integration tests can prove the
// fake clock is wired through DI rather than the endpoint reading the system clock.
app.MapGet("/time", (TimeProvider clock) => Results.Json(new { utcNow = clock.GetUtcNow() }));

app.Run();
using Application;

using Infrastructure;

using ServiceDefaults;

using Web.Api.Composition;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register: service defaults, then the Application and Infrastructure roots.
builder.AddServiceDefaults();
builder.AddApplication();
builder.AddInfrastructure();

// Register: the HTTP surface and its feature modules.
builder.AddApi();

WebApplication app = builder.Build();

// Initialize: warm application services and run the persistence pipeline.
app.UseInfrastructure();

// Map: Carter modules, health endpoints, and the clock probe.
app.UseApi();
app.MapDefaultEndpoints();

app.Run();

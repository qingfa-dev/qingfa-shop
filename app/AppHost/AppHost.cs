using Aspire.Hosting.JavaScript;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Initialize: AppHost owns the same backing services Compose runs, so F5 and the
// container stack exercise the same ConnectionStrings__db/__redis keys.
var db = builder
    .AddPostgres("postgres")
    .AddDatabase("db");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.Web_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq)
    .WithEnvironment("EfCore__ConnectionStringName", "db")
    .WithEnvironment("Persistence__AutoMigrate", "true")
    .WithHttpHealthCheck("/health");

// Enforce: the gateway fronts the Api, so every browser request enters here rather
// than reaching the Api directly.
IResourceBuilder<ProjectResource> gateway = builder
    .AddProject<Projects.Gateway>("gateway")
    .WithReference(api)
    .WithHttpHealthCheck("/health");

// Enforce: the storefront is served at the gateway root. The apps are addressed
// only through the gateway, so they take API_BASE_URL without referencing it back
// (a back reference would create a dependency cycle).
IResourceBuilder<ViteAppResource> shopfront = builder
    .AddViteApp("shopfront", "../Web.Storefront")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithEnvironment("API_BASE_URL", gateway.GetEndpoint("http"));

// Enforce: the admin dashboard is served under /admin on the same origin.
IResourceBuilder<ViteAppResource> admin = builder
    .AddViteApp("admin", "../Web.Admin")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithEnvironment("API_BASE_URL", gateway.GetEndpoint("http"));

gateway
    .WithReference(shopfront)
    .WithReference(admin);

builder.Build().Run();
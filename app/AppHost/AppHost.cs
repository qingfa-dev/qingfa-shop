using AppHost;

using Aspire.Hosting.JavaScript;

using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Initialize: AppHost owns the same backing services Compose runs, so F5 and the
// container stack exercise the same ConnectionStrings__db/__redis keys.
var db = builder
    .AddPostgres("postgres")
    .AddDatabase("db");

var redis = builder.AddRedis("redis");

// Initialize: the broker is a backing service like PostgreSQL and Redis; the named
// volume keeps queues across AppHost restarts. Compose declares its own volume.
var rabbitmq = builder
    .AddRabbitMQ("rabbitmq")
    .WithDataVolume(AppHostConstant.Volumes.RabbitMqData);

// Initialize: forward the payments configuration so `dotnet run` and Compose agree.
// Secrets are never written here; they arrive through the environment.
string stripeEnabled = builder.Configuration["Payments:Providers:Stripe:Enabled"] ?? "false";
string paypalEnabled = builder.Configuration["Payments:Providers:PayPal:Enabled"] ?? "false";
string intentLifetime = builder.Configuration["Payments:Policy:HostedCheckoutIntentLifetime"] ?? "00:30:00";
string expiryGrace = builder.Configuration["Payments:Policy:ProviderExpiryGracePeriod"] ?? "00:02:00";

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
    .WithEnvironment("Messaging__Transport", "RabbitMq")
    .WithEnvironment("Payments__Providers__Stripe__Enabled", stripeEnabled)
    .WithEnvironment("Payments__Providers__PayPal__Enabled", paypalEnabled)
    .WithEnvironment("Payments__Policy__HostedCheckoutIntentLifetime", intentLifetime)
    .WithEnvironment("Payments__Policy__ProviderExpiryGracePeriod", expiryGrace)
    .WithHttpHealthCheck("/health");

// Register: each configured currency ceiling becomes its own environment key.
foreach (IConfigurationSection currency in builder.Configuration
    .GetSection("Payments:Policy:MaximumAmountsByCurrency")
    .GetChildren())
{
    api.WithEnvironment(
        $"Payments__Policy__MaximumAmountsByCurrency__{currency.Key}",
        currency.Value ?? "0");
}

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
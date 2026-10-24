IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Initialize: AppHost owns the same backing services Compose runs, so F5 and the
// container stack exercise the same ConnectionStrings__db/__redis keys.
var db = builder
    .AddPostgres("postgres")
    .AddDatabase("db");

var redis = builder.AddRedis("redis");

IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.Web_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
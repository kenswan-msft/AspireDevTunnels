IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> apiService =
    builder.AddProject<Projects.AspireDevTunnels_ApiService>("apiservice");

builder.AddProject<Projects.AspireDevTunnels_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

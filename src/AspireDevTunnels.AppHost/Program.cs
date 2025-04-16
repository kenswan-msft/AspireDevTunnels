using AspireDevTunnels.AppHost.Extensions;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> apiService =
    builder.AddProject<Projects.AspireDevTunnels_ApiService>("apiservice");

// Feature Entry
// You can view this entry at "https://apiservice-tunnel-<port>.<region>.devtunnels.ms/openapi/v1.json"
apiService.WithDevTunnel(port: 1234, region: "use");

builder.AddProject<Projects.AspireDevTunnels_Web>("webfrontend")
    // you can view this entry at "https://webfrontend-tunnel-<port>.<region>.devtunnels.ms"
    // .WithDevTunnel(port: 1235, region: "use")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

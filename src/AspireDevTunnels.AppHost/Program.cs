using AspireDevTunnels.AppHost.Extensions;
using AspireDevTunnels.AppHost.Resources;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Creation of Dev Tunnel
IResourceBuilder<DevTunnelResource> devTunnelResource =
    builder.AddDevTunnel("aspire-tunnel");
// Uncomment to turn off auto-start
// .WithExplicitStart();

IResourceBuilder<ProjectResource> apiService =
    builder.AddProject<Projects.AspireDevTunnels_ApiService>("apiservice")
        // DevTunnel Port Binding
        .WithDevTunnel(devTunnelResource);

builder.AddProject<Projects.AspireDevTunnels_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    // .WithDevTunnel(devTunnelResource)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

using AspireDevTunnels.AppHost.Extensions;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Creation of Dev Tunnel
IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder =
    builder.AddDevTunnel("aspire-tunnel");

IResourceBuilder<ProjectResource> apiService =
    builder.AddProject<Projects.AspireDevTunnels_ApiService>("apiservice")
        // DevTunnel Port Binding
        .WithDevTunnel(devTunnelResourceBuilder);

builder.AddProject<Projects.AspireDevTunnels_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    // .WithDevTunnel(devTunnelResourceBuilder)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

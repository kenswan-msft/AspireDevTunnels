using AspireDevTunnels.AppHost.Extensions;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Creation of new Dev Tunnel
IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder =
    builder.AddDevTunnel("devtunnel", isPrivate: true);

IResourceBuilder<ProjectResource> apiService =
    builder.AddProject<Projects.AspireDevTunnels_ApiService>("apiservice")
        // DevTunnel Port Binding
        .WithDevTunnel(devTunnelResourceBuilder);

builder.AddProject<Projects.AspireDevTunnels_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

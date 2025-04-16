using AspireDevTunnels.AppHost.Extensions;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> apiService =
    builder.AddProject<Projects.AspireDevTunnels_ApiService>("apiservice");

// Feature Entry
// You can view this entry at "https://aspire-devtunnel-api-1234.usw2.devtunnels.ms/openapi/v1.json"
apiService.WithDevTunnel(options =>
{
    options.TunnelId = "aspire-devtunnel-api";
    options.Port = 1234;
    options.IsPersistent = true;
    options.DashBoardRelativeUrl = "/openapi/v1.json";
});

builder.AddProject<Projects.AspireDevTunnels_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

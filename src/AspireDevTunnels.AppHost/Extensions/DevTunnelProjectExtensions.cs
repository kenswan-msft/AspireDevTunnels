using Microsoft.Extensions.DependencyInjection;

namespace AspireDevTunnels.AppHost.Extensions;

public static class DevTunnelProjectExtensions
{
    public static IResourceBuilder<ProjectResource> WithDevTunnel(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        Action<DevTunnelOptions> configureOptions = null)
    {
        DevTunnelOptions devTunnelOptions = new();

        if (configureOptions is not null)
        {
            configureOptions(devTunnelOptions);
        }

        string tunnelName = devTunnelOptions.TunnelName ?? "sample-devtunnel-api";

        // TODO: Get port from resource launch port if no port is specified
        // This will be used for the tunnel integration port configuration
        int portNumber = devTunnelOptions.Port ?? 1234;

        if (devTunnelOptions.Port.HasValue)
        {
            resourceBuilder.WithEndpoint(tunnelName, endpoint =>
            {
                endpoint.Port = portNumber;
                endpoint.UriScheme = "https";
                endpoint.IsExternal = true;
                endpoint.IsProxied = false;
                endpoint.Name = tunnelName;
                endpoint.TargetPort = portNumber;
            });
        }

        // TODO: Get Url from Dev Tunnel Integration
        resourceBuilder.WithUrls(context =>
        {
            context.Urls.Add(new ResourceUrlAnnotation
            {
                Url = $"https://{tunnelName}-{portNumber}.devtunnels.ms",
                DisplayText = resourceBuilder.Resource.Name + " DevTunnel URL"
            });
        });

        resourceBuilder.ApplicationBuilder.Services
            // Service that handle tunnels integration
            .AddKeyedSingleton<IDevTunnelService, DevTunnelService>(tunnelName)
            .AddOptions<DevTunnelOptions>(tunnelName);

        // TODO: Add entry to configure tunnel on startup

        return resourceBuilder;
    }
}

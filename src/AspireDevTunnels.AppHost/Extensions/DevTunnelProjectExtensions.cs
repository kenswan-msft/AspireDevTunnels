namespace AspireDevTunnels.AppHost.Extensions;

public static class DevTunnelProjectExtensions
{
    public static IResourceBuilder<ProjectResource> WithDevTunnel(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        string tunnelName,
        int portNumber)
    {
        // TODO: Configure a Service/Helper
        // resourceBuilder.ApplicationBuilder.Services.AddSingleton<IDevTunnelService, DevTunnelService>();

        resourceBuilder.WithUrls(context =>
        {
            context.Urls.Add(new ResourceUrlAnnotation
            {
                Url = $"https://{tunnelName}-{portNumber}.devtunnels.ms",
                DisplayText = "DevTunnel URL"
            });
        });

        resourceBuilder.WithEndpoint(tunnelName, endpoint =>
        {
            endpoint.Port = portNumber;
            endpoint.UriScheme = "https";
            endpoint.IsExternal = true;
            endpoint.IsProxied = false;
            endpoint.Name = tunnelName;
            endpoint.TargetPort = portNumber;
        });

        // TODO: Add entry to configure tunnel on startup

        return resourceBuilder;
    }
}

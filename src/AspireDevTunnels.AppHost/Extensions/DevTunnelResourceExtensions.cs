namespace AspireDevTunnels.AppHost.Extensions
{
    public static class DevTunnelResourceExtensions
    {
        public static IResourceBuilder<ProjectResource> WithDevTunnel(
            this IResourceBuilder<ProjectResource> resourceBuilder,
            int port,
            bool isPrivate = true)
        {
            string devResourceName = $"{resourceBuilder.Resource.Name}-tunnel";

            DevTunnelResource devTunnelResource = new(
                devResourceName,
                port,
                isPrivate,
                resourceBuilder.Resource);

            // Add new Port for associated resource
            resourceBuilder.WithEndpoint(devTunnelResource.Name, endpoint =>
            {
                endpoint.Port = devTunnelResource.Port;
                // TODO: Get scheme from active launch profile
                endpoint.UriScheme = devTunnelResource.Scheme;
                endpoint.IsExternal = true;
                endpoint.IsProxied = false;
                endpoint.Name = "tunnel";
                endpoint.TargetPort = devTunnelResource.Port;
            });

            IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder = resourceBuilder.ApplicationBuilder
                .AddResource(devTunnelResource)
                .WithArgs(["host"])
                .WithReferenceRelationship(resourceBuilder.Resource);

            devTunnelResourceBuilder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(
                async (context, cancellationToken) =>
                {
                    // Initializing tunnel creation and port binding
                    // "devtunnel host" command will be executed as part of ExecutableResource process flow to start the tunnel
                    await devTunnelResource.InitializeAsync(cancellationToken);

                    devTunnelResourceBuilder
                        .WithUrl(url: devTunnelResource.TunnelUrl, displayText: devTunnelResource.Name)
                        .WithEnvironment("DEV_TUNNEL_URL", devTunnelResource.TunnelUrl);

                    if (devTunnelResource.IsPrivate)
                    {
                        string authToken = await devTunnelResource.GetAccessTokenAsync(cancellationToken);

                        devTunnelResourceBuilder
                            .WithEnvironment("DEV_TUNNEL_AUTH_HEADER_FORMAT", "X-Tunnel-Authorization: tunnel <token>")
                            .WithEnvironment("DEV_TUNNEL_TOKEN", authToken);
                    }
                    else
                    {
                        await devTunnelResource.AllowAnonymousAccessAsync(cancellationToken);
                    }

                    Console.WriteLine($"Tunnel Ready for Start: {devTunnelResource.TunnelUrl}");
                });

            return resourceBuilder;
        }
    }
}

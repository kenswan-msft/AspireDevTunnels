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

            //devTunnelResource.Annotations.Add(new EnvironmentCallbackAnnotation("TUNNEL_URL", () => devTunnelResource.TunnelUrl));
            //devTunnelResource.Annotations.Add(new ResourceUrlAnnotation { Url = devTunnelResource.TunnelUrl, DisplayText = $"{devResourceName} Url" });

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
                .WithReferenceRelationship(resourceBuilder.Resource);

            devTunnelResourceBuilder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(
                async (context, cancellationToken) =>
                {
                    // Start the tunnel
                    await devTunnelResource.StartAsync(cancellationToken);

                    // TODO: URL Not Showing in Dashboard
                    devTunnelResource.Annotations.Add(
                        new EnvironmentCallbackAnnotation("TUNNEL_URL", () => devTunnelResource.TunnelUrl));

                    if (devTunnelResource.IsPrivate)
                    {
                        string authToken = await devTunnelResource.GetAccessTokenAsync(cancellationToken);

                        // TODO: Env variables not showing in dashboard
                        devTunnelResource.Annotations.Add(new EnvironmentCallbackAnnotation("TUNNEL_TOKEN", () => authToken));
                    }
                    else
                    {
                        await devTunnelResource.AllowAnonymousAccessAsync(cancellationToken);
                    }

                    Console.WriteLine($"Tunnel Ready At: {devTunnelResource.TunnelUrl}");
                });

            return resourceBuilder;
        }
    }
}

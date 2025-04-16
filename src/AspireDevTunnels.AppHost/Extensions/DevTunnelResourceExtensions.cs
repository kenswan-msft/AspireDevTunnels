namespace AspireDevTunnels.AppHost.Extensions
{
    public static class DevTunnelResourceExtensions
    {
        public static IResourceBuilder<ProjectResource> WithDevTunnel(
            this IResourceBuilder<ProjectResource> resourceBuilder,
            int port,
            string region)
        {
            string devResourceName = $"{resourceBuilder.Resource.Name}-tunnel";

            DevTunnelResource devTunnelResource = new(
                devResourceName,
                port,
                region,
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

            resourceBuilder.ApplicationBuilder
                .AddResource(devTunnelResource)
                // TODO: URL Not Showing in Dashboard
                .WithUrl(displayText: $"{devResourceName} Url", url: devTunnelResource.TunnelUrl)
                // TODO: Env variables not showing in dashboard
                .WithEnvironment("TUNNEL_URL", devTunnelResource.TunnelUrl)
                .WithEnvironment("TUNNEL_TOKEN", "N/A")
                .WithReferenceRelationship(resourceBuilder.Resource);

            resourceBuilder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(
                async (context, cancellationToken) =>
                {
                    // Start the tunnel
                    await devTunnelResource.StartAsync(cancellationToken);

                    // IResource devTunnelResource = context.Model.Resources.First(resource => resource.Name == devResourceName);
                    // resource.Annotations.Add(new EnvironmentAnnotation("TUNNEL_TOKEN", ""));

                    Console.WriteLine($"Tunnel URL: {devTunnelResource.TunnelUrl}");
                });

            return resourceBuilder;
        }
    }
}

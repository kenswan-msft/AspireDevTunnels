namespace AspireDevTunnels.AppHost.Extensions
{
    public static class DevTunnelResourceExtensions
    {
        public static IResourceBuilder<DevTunnelResource> AddDevTunnel(
            this IDistributedApplicationBuilder builder,
            string name,
            bool isPrivate = true)
        {
            DevTunnelResource devTunnelResource = new(name, isPrivate);

            IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder = builder.AddResource(devTunnelResource)
                .WithArgs(["host"]);

            builder.Eventing.Subscribe<BeforeResourceStartedEvent>(devTunnelResource,
                async (context, cancellationToken) =>
                {
                    // Initializing tunnel creation
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
                        // Add Button to make anonymous
                        await devTunnelResource.AllowAnonymousAccessAsync(cancellationToken);
                    }

                    Console.WriteLine($"Tunnel Ready for Start: {devTunnelResource.TunnelUrl}");
                });

            return devTunnelResourceBuilder;
        }

        public static IResourceBuilder<T> WithDevTunnel<T>(
            this IResourceBuilder<T> resourceBuilder,
            IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder)
                where T : IResourceWithEndpoints
        {
            // TODO: Check port limits

            // Add new Port for associated resource
            IEnumerable<EndpointReference> endpoints = resourceBuilder.Resource.GetEndpoints()
                .Where(endpoint => endpoint.Scheme == "https");

            if (!endpoints.Any())
            {
                throw new InvalidOperationException("No HTTPS endpoints found to host.");
            }

            devTunnelResourceBuilder.WithParentRelationship(resourceBuilder.Resource);

            devTunnelResourceBuilder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(devTunnelResourceBuilder.Resource,
                async (context, cancellationToken) =>
                {
                    foreach (EndpointReference endpoint in endpoints)
                    {
                        // Add port to tunnel
                        await devTunnelResourceBuilder.Resource.AddPortAsync(
                            endpoint.Port,
                            endpoint.Scheme,
                            cancellationToken);
                    }
                });

            return resourceBuilder;
        }
    }
}

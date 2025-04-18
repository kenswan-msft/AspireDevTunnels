namespace AspireDevTunnels.AppHost.Extensions;

public static class DevTunnelResourceExtensions
{
    public static IResourceBuilder<DevTunnelResource> AddDevTunnel(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        DevTunnelResource devTunnelResource = new(name);

        IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder =
            builder
                .AddResource(devTunnelResource)
                .WithArgs(["host"]);

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(devTunnelResource,
            async (context, cancellationToken) =>
            {
                // Initializing tunnel creation
                await devTunnelResource.InitializeAsync(cancellationToken);

                devTunnelResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation("DEV_TUNNEL_URL", () => devTunnelResource.TunnelUrl));

                string authToken = await devTunnelResource.GetAccessTokenAsync(cancellationToken);

                devTunnelResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(env =>
                    {
                        env.Add("DEV_TUNNEL_AUTH_HEADER", "X-Tunnel-Authorization");
                        env.Add("DEV_TUNNEL_AUTH_TOKEN", authToken);
                    }));

                Console.WriteLine($"Tunnel Initialized: {devTunnelResource.TunnelUrl}");
            });

        // Add Public Tunnel Support
        devTunnelResourceBuilder
            .WithCommand("anonymous", "Make Endpoint Public", async (context) =>
             {
                 await devTunnelResource.AllowAnonymousAccessAsync(context.CancellationToken);

                 return new ExecuteCommandResult
                 {
                     Success = true,
                 };

             }, commandOptions: new CommandOptions
             {
                 ConfirmationMessage = "Are you sure you want to make the dev tunnel publicly available?"
             });

        return devTunnelResourceBuilder;
    }

    public static IResourceBuilder<T> WithDevTunnel<T>(
        this IResourceBuilder<T> resourceBuilder,
        IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder)
            where T : IResourceWithEndpoints
    {
        // TODO: Check port limits (how many ports per tunnel allowed)

        // Add new Port for associated resource
        IEnumerable<EndpointReference> endpoints = resourceBuilder.Resource.GetEndpoints()
            .Where(endpoint => endpoint.Scheme == "https");

        if (!endpoints.Any())
        {
            throw new InvalidOperationException("No HTTPS endpoints found to host.");
        }

        devTunnelResourceBuilder.WithParentRelationship(resourceBuilder.Resource);

        DevTunnelResource devTunnelResource = devTunnelResourceBuilder.Resource;

        string resourceUrlKey = resourceBuilder.Resource.Name.ToUpper() + "_URL";

        devTunnelResourceBuilder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            devTunnelResource,
            async (context, cancellationToken) =>
            {
                foreach (EndpointReference endpoint in endpoints)
                {
                    // Add port to tunnel
                    DevTunnelActivePort devTunnelActivePort =
                        await devTunnelResource.AddPortAsync(
                            endpoint.Port,
                            endpoint.Scheme,
                            cancellationToken);

                    // TODO: URL Not Showing in URL Column, adding to Env Variables for now
                    devTunnelResource.Annotations.Add(
                        new ResourceUrlsCallbackAnnotation(c => c.Urls.Add(new() { DisplayText = resourceUrlKey, Url = devTunnelActivePort.PortUri })));

                    // TODO: Not populating on very first run
                    // Host command could be needed
                    devTunnelResource.Annotations.Add(
                        new EnvironmentCallbackAnnotation(resourceUrlKey, () => devTunnelActivePort.PortUri));
                }
            });

        return resourceBuilder;
    }
}

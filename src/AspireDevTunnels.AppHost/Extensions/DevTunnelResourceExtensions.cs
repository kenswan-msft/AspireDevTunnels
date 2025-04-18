namespace AspireDevTunnels.AppHost.Extensions;

public static class DevTunnelResourceExtensions
{
    public static IResourceBuilder<DevTunnelResource> AddDevTunnel(
        this IDistributedApplicationBuilder builder,
        string name,
        bool autoStart = false)
    {
        DevTunnelResource devTunnelResource = new(name);

        IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder =
            builder
                .AddResource(devTunnelResource)
                .WithArgs(["host"]);

        bool autoStartTriggered = true;

        if (!autoStart)
        {
            devTunnelResourceBuilder.WithExplicitStart();
            autoStartTriggered = false;
        }

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            devTunnelResource,
            async (context, cancellationToken) =>
            {
                // When AutoStart is disabled, the BeforeResourceStartedEvent lifecycle event
                // gets triggered twice. Once at startup and once when the resource is started.
                if (autoStartTriggered == false || devTunnelResource.IsInitialized)
                {
                    Console.WriteLine("Tunnel already initialized.");

                    autoStartTriggered = true;

                    return;
                }

                await InitializeTunnelAsync(devTunnelResource, cancellationToken);
                await InitializePortsAsync(devTunnelResource, cancellationToken);
            });

        // Add Public Tunnel Support
        devTunnelResourceBuilder
            .WithCommand("anonymous-access", "Make Endpoint Public", async (context) =>
             {
                 await devTunnelResource.AllowAnonymousAccessAsync(context.CancellationToken);

                 return new ExecuteCommandResult
                 {
                     Success = true,
                 };

             }, commandOptions: new CommandOptions
             {
                 ConfirmationMessage = "Are you sure you want to make the dev tunnel publicly available?",
                 Parameter = true,
                 IconName = "Play",
                 UpdateState = (updateState) =>
                 {
                     return updateState.ResourceSnapshot.State?.Text != "Running" ? ResourceCommandState.Disabled : ResourceCommandState.Enabled;
                 },
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

        return resourceBuilder;
    }

    private static async Task InitializeTunnelAsync(
        DevTunnelResource devTunnelResource,
        CancellationToken cancellationToken)
    {
        await devTunnelResource.InitializeAsync(cancellationToken);

        devTunnelResource.Annotations.Add(
            new EnvironmentCallbackAnnotation(
                name: "DEV_TUNNEL_URL",
                callback: () => devTunnelResource.TunnelUrl));

        string authToken = await devTunnelResource.GetAccessTokenAsync(cancellationToken);

        devTunnelResource.Annotations.Add(
            new EnvironmentCallbackAnnotation(env =>
            {
                env.Add("DEV_TUNNEL_AUTH_HEADER", "X-Tunnel-Authorization");
                env.Add("DEV_TUNNEL_AUTH_TOKEN", authToken);
            }));

        Console.WriteLine($"Tunnel Initialized: {devTunnelResource.TunnelUrl}");
    }

    private static async Task InitializePortsAsync(
        DevTunnelResource devTunnelResource,
        CancellationToken cancellationToken)
    {
        var parentResources =
                    devTunnelResource.Annotations.OfType<ResourceRelationshipAnnotation>()
                        .Where(resourceRelationship => resourceRelationship.Type == "Parent")
                        .Select(resourceRelationship => resourceRelationship.Resource)
                        .ToList();

        foreach (IResource parentResource in parentResources)
        {
            bool foundEndpoints = parentResource.TryGetEndpoints(out IEnumerable<EndpointAnnotation> parentEndpoints);

            if (!foundEndpoints)
            {
                continue;
            }

            var endpoints = parentEndpoints.Where(endpoint => endpoint.UriScheme == "https").ToList();

            foreach (EndpointAnnotation endpoint in endpoints)
            {
                if (!endpoint.Port.HasValue)
                {
                    Console.WriteLine($"No eligible port found for {endpoint.Name}.");

                    continue;
                }

                string resourceUrlKey = parentResource.Name.ToUpper() + "_URL";

                // Add port to tunnel
                DevTunnelActivePort devTunnelActivePort =
                    await devTunnelResource.AddPortAsync(
                        endpoint.Port.Value,
                        endpoint.UriScheme,
                        cancellationToken);

                devTunnelResource.Annotations.Add(
                    new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp)
                    {
                        Port = devTunnelActivePort.PortNumber,
                        TargetPort = devTunnelActivePort.PortNumber,
                        Name = resourceUrlKey,
                        IsProxied = false,
                    });

                // TODO: URL Not Showing in URL Column, adding to Env Variables for now
                devTunnelResource.Annotations.Add(
                    new ResourceUrlsCallbackAnnotation(callback =>
                        callback.Urls.Add(new() { DisplayText = resourceUrlKey, Url = devTunnelActivePort.PortUri })));

                devTunnelResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(resourceUrlKey, () => devTunnelActivePort.PortUri));

                devTunnelResource.Annotations.Add(new HealthCheckAnnotation(resourceUrlKey));
            }
        }
    }
}

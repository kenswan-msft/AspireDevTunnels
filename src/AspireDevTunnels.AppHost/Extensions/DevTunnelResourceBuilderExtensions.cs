using AspireDevTunnels.AppHost.Resources;

namespace AspireDevTunnels.AppHost.Extensions;

public static class DevTunnelResourceBuilderExtensions
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

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            devTunnelResource,
            async (context, cancellationToken) =>
            {
                // TODO: Detect Auto-Start (WithExplicitStart) and skip initialization first time if so
                await InitializeTunnelAsync(devTunnelResource, cancellationToken);
                await InitializePortsAsync(devTunnelResource, cancellationToken);
            });

        // TODO: Explore different lifecycle events for this operation
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            devTunnelResource,
            async (context, cancellationToken) =>
            {
                await AddTunnelMetadataAsync(devTunnelResource, cancellationToken);
            });

        // Add Public Tunnel Support
        devTunnelResourceBuilder
            .WithCommand("anonymous-access", "Make Endpoint Public", async (context) =>
             {
                 await devTunnelResource.AllowAnonymousAccessAsync(context.CancellationToken);

                 return new ExecuteCommandResult { Success = true };

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
        await devTunnelResource.VerifyDevTunnelCliInstalledAsync(cancellationToken);
        await devTunnelResource.VerifyUserLoggedInAsync(cancellationToken);

        // Check if tunnel already exists
        DevTunnel devTunnel = await devTunnelResource.GetTunnelDetailsAsync(cancellationToken);

        if (devTunnel is null)
        {
            await devTunnelResource.CreateTunnelAsync(cancellationToken);
        }
        else
        {
            Console.WriteLine($"Tunnel {devTunnelResource.Name} already exists. Skipping creation");
        }
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

                // Check if port already exists
                DevTunnelPort devTunnelPort =
                    await devTunnelResource.GetPortDetailsAsync(endpoint.Port.Value, cancellationToken);

                if (devTunnelPort is not null)
                {
                    Console.WriteLine($"Port {endpoint.Port.Value} already exists for tunnel {devTunnelResource.Name}");
                    continue;
                }
                else
                {
                    // Add port to tunnel
                    DevTunnelPort devTunnelActivePort =
                        await devTunnelResource.AddPortAsync(
                            endpoint.Port.Value,
                            endpoint.UriScheme,
                            cancellationToken);
                }
            }
        }
    }

    private static async Task AddTunnelMetadataAsync(DevTunnelResource devTunnelResource, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Adding tunnel metadata for {devTunnelResource.Name}...");

        DevTunnel devTunnel =
            await devTunnelResource.GetTunnelDetailsAsync(cancellationToken);

        if (devTunnel is null)
        {
            Console.WriteLine($"Tunnel {devTunnelResource.Name} not found. Skipping metadata addition.");
            return;
        }

        foreach (DevTunnelActivePort port in devTunnel.Tunnel.Ports)
        {
            // TODO: When Port added first time, URI is not available
            devTunnelResource.Annotations.Add(
                    new EnvironmentCallbackAnnotation(
                        name: $"DEV_TUNNEL_PORT_{port.PortNumber}_URL",
                        callback: () => port.PortUri));

            devTunnelResource.Annotations.Add(
                new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp)
                {
                    Port = port.PortNumber,
                    TargetPort = port.PortNumber,
                    Name = port.PortNumber.ToString(),
                    IsProxied = false,
                });

            devTunnelResource.Annotations.Add(
                new ResourceUrlsCallbackAnnotation(callback =>
                    callback.Urls.Add(new() { DisplayText = port.PortNumber.ToString(), Url = port.PortUri })));

            //devTunnelResource.Annotations.Add(new HealthCheckAnnotation(portResourceKey));
        }

        // TODO: Experiment with making this a `WithCommand` option for UI
        // Get the auth token properties for the tunnel env vars
        DevTunnelAccessInfo devTunnelAccessInfo = await devTunnelResource.GetAuthTokenAsync(cancellationToken);

        devTunnelResource.Annotations.Add(
            new EnvironmentCallbackAnnotation(env =>
            {
                env.Add("DEV_TUNNEL_AUTH_HEADER", "X-Tunnel-Authorization");
                env.Add("DEV_TUNNEL_AUTH_TOKEN", devTunnelAccessInfo.Token);
            }));
    }
}

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
                if (devTunnelResource.VerifyShouldInitialize())
                {
                    await InitializeTunnelAsync(devTunnelResource, cancellationToken);
                    await InitializePortsAsync(devTunnelResource, cancellationToken);

                    devTunnelResource.IsInitialized = true;
                }
                else
                {
                    devTunnelResource.SkippedInitializationForExplicitStart = true;
                }
            });

        devTunnelResourceBuilder
            .WithCommand("allow-anonymous-access", "Make Endpoint Public", async (context) =>
             {
                 await devTunnelResource.AllowAnonymousAccessAsync(context.CancellationToken);

                 devTunnelResource.IsPublic = true;

                 return new ExecuteCommandResult { Success = true };

             }, commandOptions: new CommandOptions
             {
                 ConfirmationMessage = "Are you sure you want to make the dev tunnel publicly available?",
                 IconName = "LockOpen",
                 UpdateState = (updateState) =>
                 {
                     return updateState.ResourceSnapshot.State?.Text != "Running" ? ResourceCommandState.Disabled : ResourceCommandState.Enabled;
                 },
             });

        devTunnelResourceBuilder
            .WithCommand("get-tunnel-urls", "Get URLs", async (context) =>
            {
                DevTunnel devTunnel =
                    await devTunnelResource.GetTunnelDetailsAsync(context.CancellationToken);

                Console.WriteLine($"Tunnel {devTunnelResource.Name} URLs:");

                foreach (DevTunnelActivePort port in devTunnel.Tunnel.Ports)
                {
                    Console.WriteLine($"Port {port.PortNumber}: {port.PortUri}");
                }

                return new ExecuteCommandResult { Success = true };

            }, commandOptions: new CommandOptions
            {
                IconName = "LinkMultiple",
                UpdateState = (updateState) =>
                {
                    return updateState.ResourceSnapshot.State?.Text != "Running" ? ResourceCommandState.Disabled : ResourceCommandState.Enabled;
                },
            });

        devTunnelResourceBuilder
           .WithCommand("get-access-token", "Get Access Token", async (context) =>
           {
               DevTunnelAccessInfo devTunnelAccessInfo =
                   await devTunnelResource.GetAuthTokenAsync(context.CancellationToken);

               Console.WriteLine($"{devTunnelResource.Name} Token (and header):");

               Console.WriteLine($"X-Tunnel-Authorization: tunnel {devTunnelAccessInfo.Token}");

               return new ExecuteCommandResult { Success = true };

           }, commandOptions: new CommandOptions
           {
               IconName = "Key",
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
}

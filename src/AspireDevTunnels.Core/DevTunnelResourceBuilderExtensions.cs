using Microsoft.DevTunnels.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspireDevTunnels.Core;

public static class DevTunnelResourceBuilderExtensions
{
    public static IResourceBuilder<DevTunnelResource> AddDevTunnel(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        DevTunnelOptions devTunnelOptions =
            builder.Configuration.GetSection(nameof(DevTunnelOptions)).Get<DevTunnelOptions>();

        DevTunnelResource devTunnelResource = new(name, devTunnelOptions.Scope, devTunnelOptions);

        IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder =
            builder
                .AddResource(devTunnelResource)
                .WithArgs("host");

        // Startup events
        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(
            devTunnelResource,
            async (context, cancellationToken) =>
            {
                await devTunnelResource.Tunnel.CreateTunnelAsync(cancellationToken);
                await InitializePortsAsync(devTunnelResource, cancellationToken);
            });

        // Dashboard Actions
        devTunnelResourceBuilder
            .WithCommand(
                "allow-anonymous-access", "Make Endpoint Public", async context =>
                {
                    await devTunnelResource.Tunnel.UpdateAccessAsync(true, context.CancellationToken);

                    devTunnelResource.IsPublic = true;

                    return new() { Success = true };
                }, new()
                {
                    ConfirmationMessage = "Are you sure you want to make the dev tunnel publicly available?",
                    IconName = "LockOpen",
                    UpdateState = updateState =>
                        updateState.ResourceSnapshot.State?.Text != "Running"
                            ? ResourceCommandState.Disabled
                            : ResourceCommandState.Enabled
                });

        devTunnelResourceBuilder
            .WithCommand(
                "get-tunnel-urls", "Get URLs", async context =>
                {
                    TunnelPort[] tunnelPorts =
                        await devTunnelResource.Tunnel.GetActivePortsAsync(context.CancellationToken);

                    Console.WriteLine($"Tunnel {devTunnelResource.Name} URLs:");

                    foreach (TunnelPort tunnelPort in tunnelPorts)
                    {
                        Console.WriteLine("------------------------------------------------------");
                        Console.WriteLine($"Tunnel URL: {tunnelPort.PortForwardingUris.First()}");
                        Console.WriteLine($"Tunnel Inspection URL: {tunnelPort.InspectionUri}");
                    }

                    return new() { Success = true };
                }, new()
                {
                    IconName = "LinkMultiple",
                    UpdateState = updateState =>
                        updateState.ResourceSnapshot.State?.Text != "Running"
                            ? ResourceCommandState.Disabled
                            : ResourceCommandState.Enabled
                });

        devTunnelResourceBuilder
            .WithCommand(
                "get-access-token", "Get Access Token", async context =>
                {
                    string accessToken =
                        await devTunnelResource.Tunnel.GetAccessTokenAsync(context.CancellationToken);

                    Console.WriteLine($"{devTunnelResource.Name} Token (and header):");

                    Console.WriteLine($"X-Tunnel-Authorization: tunnel {accessToken}");

                    return new() { Success = true };
                }, new()
                {
                    IconName = "Key",
                    UpdateState = updateState =>
                        updateState.ResourceSnapshot.State?.Text != "Running"
                            ? ResourceCommandState.Disabled
                            : ResourceCommandState.Enabled
                });

        // Establish Health Checks
        string healthCheckKey = $"DevTunnelHealth_{name}";

        builder.Services.AddHealthChecks()
            .Add(
                new(
                    healthCheckKey,
                    sp => sp.GetKeyedService<DevTunnelHealthCheck>(healthCheckKey),
                    null,
                    null,
                    TimeSpan.FromMinutes(2)));

        builder.Services.AddKeyedSingleton(healthCheckKey, new DevTunnelHealthCheck(name));

        devTunnelResourceBuilder.WithHealthCheck(healthCheckKey);

        return devTunnelResourceBuilder;
    }

    public static IResourceBuilder<T> WithDevTunnel<T>(
        this IResourceBuilder<T> resourceBuilder,
        IResourceBuilder<DevTunnelResource> devTunnelResourceBuilder)
        where T : IResourceWithEndpoints
    {
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

                TunnelPort _ =
                    await devTunnelResource.Tunnel.AddPortAsync(
                        endpoint.Port.Value,
                        endpoint.UriScheme,
                        cancellationToken);
            }
        }
    }
}

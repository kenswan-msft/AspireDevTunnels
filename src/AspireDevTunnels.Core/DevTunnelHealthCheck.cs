using AspireDevTunnels.Core.Commands;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AspireDevTunnels.Core;

internal class DevTunnelHealthCheck(string name) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Running health check for {name}...");

        // 1. Check if DevTunnel CLI is installed
        DevTunnelCommandResult isInstalled =
            await DevTunnelCommandProcessor.VerifyDevTunnelCliInstalledAsync(cancellationToken);

        if (!isInstalled.IsSuccess)
        {
            return HealthCheckResult.Unhealthy(
                "DevTunnel CLI is not installed. Please install from https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started.");
        }

        // 2. Verify User is logged in
        DevTunnelCommandResult<DevTunnelCommandProcessor.DevTunnelUserInfo>? devTunnelUser =
            await DevTunnelCommandProcessor.VerifyUserLoggedInAsync(cancellationToken);

        Console.WriteLine($"Dev Tunnel User: {devTunnelUser.Value?.Username ?? "Not logged in"}");

        // 3. Check if the tunnel exists
        DevTunnelCommandResult<DevTunnelCommandProcessor.DevTunnel> devTunnelCommandResult =
            await DevTunnelCommandProcessor.VerifyTunnelExistsAndActiveAsync(name, cancellationToken);

        if (devTunnelCommandResult.IsSuccess && devTunnelCommandResult.Value is not null)
        {
            DevTunnelCommandProcessor.DevTunnel devTunnel = devTunnelCommandResult.Value;

            Console.WriteLine($"Tunnel {name} is healthy.");

            return HealthCheckResult.Healthy(
                $"Tunnel {devTunnel.Tunnel.TunnelId} is healthy. Hosting {devTunnel.Tunnel.Ports.Count} Ports.");
        }

        return HealthCheckResult.Unhealthy(
            $"Tunnel {name} is unhealthy. Error: {devTunnelCommandResult.Error}; ExitCode: {devTunnelCommandResult.ExitCode}; Output: {devTunnelCommandResult.Output}");
    }
}

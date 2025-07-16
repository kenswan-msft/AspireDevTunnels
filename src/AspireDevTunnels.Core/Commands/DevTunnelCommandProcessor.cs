namespace AspireDevTunnels.Core.Commands;

public class DevTunnelCommandProcessor
{
    public static async Task<DevTunnelCommandResult> VerifyDevTunnelCliInstalledAsync(
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Verifying DevTunnel Resources Exist on machine...");

        List<string> commandLineArgs = DevTunnelCommandArgs.VerifyDevTunnelCliInstalledArguments();

        DevTunnelCommandResult devTunnelCommandResult =
            await DevTunnelCommands.TryRunProcessAsync(commandLineArgs, cancellationToken);

        return devTunnelCommandResult;
    }

    public static async Task<DevTunnelCommandResult<DevTunnelUserInfo>> VerifyUserLoggedInAsync(
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Verifying User is Logged in for DevTunnel usage...");

        List<string> commandLineArgs = DevTunnelCommandArgs.VerifyUserLoggedInArguments();

        DevTunnelUserInfo devTunnelUserInfo =
            await DevTunnelCommands.RunProcessAsync<DevTunnelUserInfo>(commandLineArgs, cancellationToken);

        // Exit code is still 0 when user is not logged in.
        // Sample output: { "status": "Not logged in" }
        if (devTunnelUserInfo.Status.Contains("Not logged in", StringComparison.OrdinalIgnoreCase))
        {
            return new(
                exitCode: 1,
                error: "User is not logged in. Please log in using 'devtunnel user login'.",
                value: null,
                output: devTunnelUserInfo.Status);
        }

        return new(exitCode: 0, value: devTunnelUserInfo, error: null, output: "User is logged in.");
    }

    public static async Task<DevTunnelCommandResult<DevTunnel>> VerifyTunnelExistsAndActiveAsync(
        string tunnelId,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Retrieving tunnel details for {tunnelId}...");

        List<string> commandLineArgs = DevTunnelCommandArgs.GetTunnelDetailsArguments(tunnelId);

        DevTunnelCommandResult<DevTunnel> devTunnelCommandResult =
            await DevTunnelCommands.TryRunProcessAsync<DevTunnel>(commandLineArgs, cancellationToken);

        return devTunnelCommandResult;
    }

    public record DevTunnelUserInfo(string Status, string Provider, string Username, string TenantId, string ObjectId);

    public record DevTunnelActivePort(int PortNumber, string Protocol, string PortUri);

    public record DevTunnelInfo(
        string TunnelId,
        string TunnelExpiration,
        int HostConnections,
        int ClientConnections,
        List<DevTunnelActivePort> Ports);

    public record DevTunnel(DevTunnelInfo Tunnel);
}

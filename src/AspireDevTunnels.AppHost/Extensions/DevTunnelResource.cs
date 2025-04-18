namespace AspireDevTunnels.AppHost.Extensions;

public class DevTunnelResource(string name, bool isPrivate) : ExecutableResource(name, "devtunnel", "./")
{
    private readonly IDevTunnelProvider devTunnelProvider = new MsDevTunnel();

    public string TunnelUrl => devTunnelProvider.Url;

    public bool IsPrivate => isPrivate;

    public async Task InitializeAsync(CancellationToken cancellationToken) =>
        await devTunnelProvider.CreateTunnelAsync(Name, cancellationToken);

    public async Task<DevTunnelActivePort> AddPortAsync(int port, string protocol = "https", CancellationToken cancellationToken = default)
    {
        await devTunnelProvider.AddPortAsync(port, protocol, cancellationToken);

        // Full Port Url is not given from "Add Port" payload
        // Retrieving tunnel details to get the full URL
        DevTunnel devTunnel = await devTunnelProvider.GetTunnelDetailsAsync(cancellationToken);

        DevTunnelActivePort devTunnelActivePort = devTunnel.Tunnel.Ports.FirstOrDefault(p => p.PortNumber == port);

        return devTunnelActivePort;
    }

    public async Task<DevTunnel> GetTunnelDetailsAsync(CancellationToken cancellationToken = default) =>
        await devTunnelProvider.GetTunnelDetailsAsync(cancellationToken);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        string token = await devTunnelProvider.GetAuthTokenAsync(cancellationToken);

        return token;
    }

    public async Task AllowAnonymousAccessAsync(CancellationToken cancellationToken) =>
        await devTunnelProvider.AllowAnonymousAccessAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        devTunnelProvider.StopTunnelAsync(cancellationToken);
}

public record DevTunnelInfo(string TunnelId, string TunnelExpiration, int HostConnections, int ClientConnections, List<DevTunnelActivePort> Ports);
public record DevTunnel(DevTunnelInfo Tunnel);
public record DevTunnelPortInfo(string TunnelId, int PortNumber, string Protocol, int ClientConnections);
public record DevTunnelPort(DevTunnelPortInfo Port);
public record DevTunnelActivePort(int PortNumber, string Protocol, string PortUri);
public record DevTunnelAccessInfo(string TunnelId, string Scope, string LifeTime, string Expiration, string Token);
public record DevTunnelUserInfo(string Status, string Provider, string Username, string TenantId, string ObjectId);

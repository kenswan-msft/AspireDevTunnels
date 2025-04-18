namespace AspireDevTunnels.AppHost.Resources;

public class DevTunnelResource(string name) : ExecutableResource(name, "devtunnel", "./")
{
    /// <summary>
    /// Can be used to track initialization of the tunnel
    /// (helps with "WithExplicitStart" triggering "BeforeResourceStartedEvent" lifecycle event more than once)
    /// </summary>
    public bool IsInitialized { get; set; }

    public bool SkipInitializationForAutoStart { get; set; }

    /// <summary>
    /// Can be used to track if user has toggled public access option
    /// </summary>
    public bool IsPublic { get; set; }
}

// DevTunnel CLI Response Object Models
public record DevTunnelInfo(string TunnelId, string TunnelExpiration, int HostConnections, int ClientConnections, List<DevTunnelActivePort> Ports);
public record DevTunnel(DevTunnelInfo Tunnel);
public record DevTunnelPortInfo(string TunnelId, int PortNumber, string Protocol, int ClientConnections);
public record DevTunnelPort(DevTunnelPortInfo Port);
public record DevTunnelActivePort(int PortNumber, string Protocol, string PortUri);
public record DevTunnelAccessInfo(string TunnelId, string Scope, string LifeTime, string Expiration, string Token);
public record DevTunnelUserInfo(string Status, string Provider, string Username, string TenantId, string ObjectId);

namespace AspireDevTunnels.Core;

public class DevTunnelResource(string name, string scope, DevTunnelOptions devTunnelOptions)
    : ExecutableResource(name, "devtunnel", "./")
{
    /// <summary>
    ///     Can be used to track if user has toggled public access option
    /// </summary>
    public bool IsPublic { get; set; }

    internal MicrosoftDevTunnel Tunnel { get; } = new(name, scope, devTunnelOptions);
}

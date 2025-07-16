namespace AspireDevTunnels.Core;

public class DevTunnelResource(string name, string scope) : ExecutableResource(name, "devtunnel", "./")
{
    /// <summary>
    ///     Can be used to track initialization of the tunnel
    ///     (helps with "WithExplicitStart" triggering "BeforeResourceStartedEvent" lifecycle event more than once)
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    ///     Can be used to track the need to skip initialization of the tunnel
    ///     (helps with "WithExplicitStart" triggering "BeforeResourceStartedEvent" lifecycle event more than once)
    /// </summary>
    public bool SkippedInitializationForExplicitStart { get; set; }

    /// <summary>
    ///     Can be used to track if user has toggled public access option
    /// </summary>
    public bool IsPublic { get; set; }

    internal MicrosoftDevTunnel Tunnel { get; } = new(name, scope);
}

namespace AspireDevTunnels.AppHost.Extensions
{
    internal class DevTunnelResource : ExecutableResource
    {
        private readonly IDevTunnelProvider devTunnelProvider = new MsDevTunnel();

        public DevTunnelResource(string name, int port, bool isPrivate, ProjectResource projectResource) :
            base(name, "devtunnel", "./")
        {
            Port = port;
            IsPrivate = isPrivate;
            AssociatedProjectResource = projectResource;
        }

        public int Port { get; }

        public string Scheme { get; } = "https";

        public string TunnelUrl => devTunnelProvider.Url;

        public bool IsPrivate { get; }

        public ProjectResource AssociatedProjectResource { get; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await devTunnelProvider.CreateTunnelAsync(Name, cancellationToken);
            await devTunnelProvider.AddPortAsync(Port, Scheme, cancellationToken);
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            string token = await devTunnelProvider.GetAuthTokenAsync(cancellationToken);

            return token;
        }

        public async Task AllowAnonymousAccessAsync(CancellationToken cancellationToken)
        {
            await devTunnelProvider.AllowAnonymousAccessAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return devTunnelProvider.StopTunnelAsync(cancellationToken);
        }
    }
}

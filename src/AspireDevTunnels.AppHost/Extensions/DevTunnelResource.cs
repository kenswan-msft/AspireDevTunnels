namespace AspireDevTunnels.AppHost.Extensions
{
    public class DevTunnelResource : ExecutableResource
    {
        private readonly IDevTunnelProvider devTunnelProvider = new MsDevTunnel();

        public DevTunnelResource(string name, bool isPrivate) :
            base(name, "devtunnel", "./")
        {
            IsPrivate = isPrivate;
        }

        public string TunnelUrl => devTunnelProvider.Url;

        public bool IsPrivate { get; }

        public ProjectResource AssociatedProjectResource { get; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await devTunnelProvider.CreateTunnelAsync(Name, cancellationToken);
        }

        public async Task AddPortAsync(int port, string protocol = "https", CancellationToken cancellationToken = default)
        {
            await devTunnelProvider.AddPortAsync(port, protocol, cancellationToken);
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

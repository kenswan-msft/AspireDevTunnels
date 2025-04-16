namespace AspireDevTunnels.AppHost.Extensions
{
    internal class DevTunnelResource : Resource, IResourceWithEnvironment, IResourceWithEndpoints
    {
        private readonly IDevTunnelService devTunnelService = new DevTunnelService();

        public DevTunnelResource(string name, int port, string region, bool isPrivate, ProjectResource projectResource) :
            base(name)
        {
            TunnelName = name;
            Port = port;
            Region = region;
            IsPrivate = isPrivate;
            TunnelUrl = $"https://{Name}-{port}.{region}.devtunnels.ms";
            AssociatedProjectResource = projectResource;
        }

        public int Port { get; }

        public string TunnelName { get; }

        public string Scheme { get; } = "https";

        public string Region { get; }

        public string TunnelUrl { get; }

        public bool IsPrivate { get; }

        public ProjectResource AssociatedProjectResource { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            devTunnelService.CreateTunnel(TunnelName);
            devTunnelService.AddPort(Port, Scheme);
            devTunnelService.StartTunnel();

            return Task.CompletedTask;
        }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            string token = devTunnelService.GetAuthToken(TunnelName);

            return Task.FromResult(token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            devTunnelService.StopTunnel();

            return Task.CompletedTask;
        }
    }
}

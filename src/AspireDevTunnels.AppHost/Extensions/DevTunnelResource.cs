namespace AspireDevTunnels.AppHost.Extensions
{
    internal class DevTunnelResource : Resource, IResourceWithEnvironment
    {

        public DevTunnelResource(string name, int port, string region, ProjectResource projectResource) :
            base(name)
        {
            TunnelName = name;
            Port = port;
            Region = region;
            TunnelUrl = $"https://{Name}-{port}.{region}.devtunnels.ms";
            AssociatedProjectResource = projectResource;
        }

        public int Port { get; }

        public string TunnelName { get; }

        public string Scheme { get; } = "https";

        public string Region { get; }

        public string TunnelUrl { get; }

        public ProjectResource AssociatedProjectResource { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            DevTunnelService devTunnelService = new();

            devTunnelService.CreateTunnel(TunnelName);
            devTunnelService.AddPort(Port, Scheme);
            devTunnelService.StartTunnel();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            DevTunnelService devTunnelService = new();

            devTunnelService.StopTunnel();

            return Task.CompletedTask;
        }
    }
}

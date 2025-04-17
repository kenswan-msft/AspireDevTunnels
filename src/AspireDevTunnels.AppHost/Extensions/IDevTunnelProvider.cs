namespace AspireDevTunnels.AppHost.Extensions
{
    public interface IDevTunnelProvider
    {
        string Id { get; }

        string Name { get; }

        string Url { get; }

        Task CreateTunnelAsync(string tunnelId, CancellationToken cancellationToken = default);

        Task AddPortAsync(int port, string protocol = "https", CancellationToken cancellationToken = default);

        Task<string> GetAuthTokenAsync(CancellationToken cancellationToken = default);

        Task AllowAnonymousAccessAsync(CancellationToken cancellationToken = default);

        Task StartTunnelAsync(CancellationToken cancellationToken = default);

        Task StopTunnelAsync(CancellationToken cancellationToken = default);
    }
}

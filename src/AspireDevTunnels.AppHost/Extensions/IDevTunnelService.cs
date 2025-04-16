namespace AspireDevTunnels.AppHost.Extensions
{
    public interface IDevTunnelService
    {
        Task CreateTunnelAsync(string tunnelName, CancellationToken cancellationToken = default);

        Task AddPortAsync(int portNumber, string protocol = "https", CancellationToken cancellationToken = default);

        Task<string> GetAuthTokenAsync(string tunnelName, CancellationToken cancellationToken = default);

        Task AllowAnonymousAccessAsync(string tunnelName, CancellationToken cancellationToken = default);

        Task StartTunnelAsync(CancellationToken cancellationToken = default);

        Task StopTunnelAsync(CancellationToken cancellationToken = default);
    }
}

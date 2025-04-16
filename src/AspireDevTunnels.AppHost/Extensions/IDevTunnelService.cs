namespace AspireDevTunnels.AppHost.Extensions
{
    public interface IDevTunnelService
    {
        void CreateTunnel(string tunnelName);

        void AddPort(int portNumber, string protocol = "https");

        void StartTunnel();

        void StopTunnel();
    }
}

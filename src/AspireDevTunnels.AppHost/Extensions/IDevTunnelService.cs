namespace AspireDevTunnels.AppHost.Extensions
{
    public interface IDevTunnelService
    {
        void CreateTunnel(string tunnelName);

        void AddPort(int portNumber, string protocol = "https");

        string GetAuthToken(string tunnelName);

        void StartTunnel();

        void StopTunnel();
    }
}

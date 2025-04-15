namespace AspireDevTunnels.AppHost.Extensions
{
    public class DevTunnelOptions
    {
        public string TunnelName { get; set; }

        public int? Port { get; set; }

        public bool IsPersistent { get; set; } = false;

        public bool IsPublic { get; set; } = true;
    }
}

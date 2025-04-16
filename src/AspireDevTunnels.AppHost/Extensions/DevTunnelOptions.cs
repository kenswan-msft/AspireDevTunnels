namespace AspireDevTunnels.AppHost.Extensions
{
    public class DevTunnelOptions
    {
        public string TunnelId { get; set; }

        public int? Port { get; set; }

        public bool IsPersistent { get; set; } = false;

        public bool IsPublic { get; set; } = true;

        public string DashBoardRelativeUrl { get; set; } = "/";
    }
}

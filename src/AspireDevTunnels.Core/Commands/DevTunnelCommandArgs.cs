namespace AspireDevTunnels.Core.Commands;

internal static class DevTunnelCommandArgs
{
    public static List<string> GetTunnelDetailsArguments(string tunnelId) =>
        ["show", tunnelId, "--json"];

    public static List<string> VerifyDevTunnelCliInstalledArguments() =>
        ["--version"];

    public static List<string> VerifyUserLoggedInArguments() =>
        ["user", "show", "--json"];
}

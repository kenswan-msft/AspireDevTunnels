using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace AspireDevTunnels.AppHost.Extensions;

public static class DevTunnelProjectExtensions
{
    public static IResourceBuilder<ProjectResource> WithDevTunnel(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        Action<DevTunnelOptions> configureOptions = null)
    {
        DevTunnelOptions devTunnelOptions = new();

        if (configureOptions is not null)
        {
            configureOptions(devTunnelOptions);
        }

        string tunnelName = devTunnelOptions.TunnelId ?? "aspire-devtunnel-api";

        // TODO: Dynamic port generation if not specified
        int portNumber = devTunnelOptions.Port ?? 1234;

        // TODO: Detect current region from the devtunnel CLI output
        string region = devTunnelOptions.Region ?? "usw2";

        string tunnelUrl = $"https://{tunnelName}-{portNumber}.{devTunnelOptions.Region}.devtunnels.ms";

        resourceBuilder.WithEndpoint(tunnelName, endpoint =>
        {
            endpoint.Port = portNumber;
            // TODO: Get scheme from active launch profile
            endpoint.UriScheme = "https";
            endpoint.IsExternal = true;
            endpoint.IsProxied = false;
            endpoint.Name = tunnelName;
            endpoint.TargetPort = portNumber;
        });

        resourceBuilder.WithUrls(context =>
        {
            context.Urls.Add(new ResourceUrlAnnotation
            {
                Url = tunnelUrl + devTunnelOptions.DashBoardRelativeUrl,
                DisplayText = resourceBuilder.Resource.Name + " Tunnel"
            });
        });

        resourceBuilder.ApplicationBuilder.Services
            // Service that handle tunnels integration
            .AddKeyedSingleton<IDevTunnelService, DevTunnelService>(tunnelName)
            .AddOptions<DevTunnelOptions>(tunnelName);

        // TODO: Add operations to resource startup event bindings
        CreateTunnel(tunnelName);
        AddPort(portNumber, "https");
        RunHost();

        return resourceBuilder;
    }

    private static void CreateTunnel(string tunnelName)
    {
        List<string> commandLineArgs = [
            "create",
            tunnelName,
            "--json"
        ];

        RunProcess(commandLineArgs);
    }

    private static void AddPort(int portNumber, string protocol = "https")
    {
        List<string> commandLineArgs = [
            "port",
            "add",
            "-p",
            portNumber.ToString(),
            "--protocol",
            protocol
        ];

        RunProcess(commandLineArgs);
    }

    private static void RunHost()
    {
        List<string> commandLineArgs = [
            "host",
        ];

        RunProcess(commandLineArgs);
    }

    private static void RunProcess(List<string> commandLineArgs)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "devtunnel",
            Arguments = string.Join(" ", commandLineArgs),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = new()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine("Error: " + e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit(milliseconds: 5000);
    }
}

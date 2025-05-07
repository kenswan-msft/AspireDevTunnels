using Azure.Core;
using Microsoft.DevTunnels.Contracts;
using Microsoft.DevTunnels.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.NamingConventionBinder;

namespace AspireDevTunnels.SDK.Commands;

public class Program
{
    public static void Main(string[] args) => BuildCommandLine()
        .UseHost(_ => Host.CreateDefaultBuilder(),
            hostBuilder =>
            {
                hostBuilder.ConfigureAppConfiguration((hostBuilderContext, configuration) =>
                {
                    configuration.AddJsonFile("appsettings.json", optional: false);
                    configuration.AddJsonFile("appsettings.Development.json", optional: true);
                });

                // Required for "ConfigureServices"
                hostBuilder.UseServiceProviderFactory(new DefaultServiceProviderFactory());

                hostBuilder.ConfigureServices((hostBuilderContext, services) =>
                {
                    services.AddScoped<TunnelAuthorizationProvider>();
                });
            })
        .Invoke(args);

    private static CommandLineConfiguration BuildCommandLine()
    {
        var root = new RootCommand(@"$ dotnet run list") { };

        // Authenticate to DevTunnels Scope
        var authenticationCommand = new Command("auth", "Set access token for subsequent requests")
        {
            Action = CommandHandler.Create<IHost>(AuthenticateAsync)
        };
        root.Subcommands.Add(authenticationCommand);

        // List Available DevTunnels
        var listTunnelsCommand = new Command("list", "List Configured DevTunnels")
        {
            Action = CommandHandler.Create<IHost>(ListDevTunnelsAsync)
        };
        root.Subcommands.Add(listTunnelsCommand);

        // Create Dev Tunnel
        var createTunnelCommand = new Command("create", "Create Dev Tunnel")
        {
            new Option<string>("--id", ["-i"]) {
                Required = true
            },
        };
        createTunnelCommand.Action = CommandHandler.Create<string, IHost>(CreateTunnelAsync);
        root.Subcommands.Add(createTunnelCommand);

        // Add Port to Dev Tunnel
        var addPortCommand = new Command("add-port", "Add Port to DevTunnel")
        {
            new Option<string>("--id", ["-i"]) {
                Required = true
            },
             new Option<string>("--cluster", ["-c"]) {
                Required = true
            },
            new Option<int>("--port", ["-p"]) {
                Required = true
            },
        };
        addPortCommand.Action = CommandHandler.Create<string, string, int, IHost>(AddPortAsync);
        root.Subcommands.Add(addPortCommand);

        // Get Access Token
        var accessTokenCommand = new Command("token", "Get DevTunnel Access Token")
        {
            new Option<string>("--id", ["-i"]) {
                Required = true
            },
            new Option<string>("--cluster", ["-c"]) {
                Required = true
            },
        };
        accessTokenCommand.Action = CommandHandler.Create<string, string, IHost>(GetAccessTokenAsync);
        root.Subcommands.Add(accessTokenCommand);

        // Get Access Token
        var accessCommand = new Command("access", "Make DevTunnel Public or Private")
        {
            new Option<string>("--id", ["-i"]) {
                Required = true
            },
            new Option<string>("--cluster", ["-c"]) {
                Required = true
            },
            new Option<bool>("--public") {
                Required = false,
            }
        };
        accessCommand.Action = CommandHandler.Create<string, string, bool, IHost>(ControlAccessAsync);
        root.Subcommands.Add(accessCommand);

        return new CommandLineConfiguration(root);
    }

    private static async Task<int> AuthenticateAsync(IHost host)
    {
        try
        {
            TunnelAuthorizationProvider tunnelAuthorizationProvider =
                host.Services.GetRequiredService<TunnelAuthorizationProvider>();

            AccessToken accessToken = await tunnelAuthorizationProvider.RefreshAuthorizationTokenAsync();

            Console.WriteLine($"Authenticated User Token: {accessToken.Token}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");

            return 1;
        }
    }

    private static async Task<int> ListDevTunnelsAsync(IHost host)
    {
        try
        {
            TunnelManagementClient tunnelManagementClient = RetrieveTunnelManagementClient(host);

            Tunnel[] tunnels =
                await tunnelManagementClient.ListTunnelsAsync(
                    clusterId: null,
                    domain: null,
                    options: null,
                    ownedTunnelsOnly: true, default);

            Console.WriteLine("Tunnels owned by the current user:");

            foreach (Tunnel tunnel in tunnels)
            {
                Console.WriteLine($"Tunnel ID: {tunnel.TunnelId}, Cluster ID: {tunnel.ClusterId}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");

            return 1;
        }
    }

    private static async Task<int> CreateTunnelAsync(string id, IHost host)
    {
        try
        {
            var tunnelRequest = new Tunnel
            {
                TunnelId = id,
                Endpoints = [],
                Ports = [],
            };

            TunnelManagementClient tunnelManagementClient = RetrieveTunnelManagementClient(host);

            Tunnel tunnel = await tunnelManagementClient.CreateOrUpdateTunnelAsync(tunnelRequest, null, default);

            Console.WriteLine($"Tunnel '{tunnel.TunnelId}' has been created on cluster {tunnel.ClusterId}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tunnel: {ex.Message}");

            return 1;
        }
    }

    private static async Task<int> AddPortAsync(string id, string cluster, int port, IHost host)
    {
        try
        {
            var tunnelRequest = new Tunnel
            {
                TunnelId = id,
                ClusterId = cluster,
                Endpoints = [],
                Ports = [],
            };

            TunnelManagementClient tunnelManagementClient = RetrieveTunnelManagementClient(host);

            Tunnel tunnel =
                await tunnelManagementClient.GetTunnelAsync(tunnelRequest, null, default);

            // Define the port mapping
            var tunnelPortRequest = new TunnelPort
            {
                PortNumber = (ushort)port,
                Protocol = TunnelProtocol.Http,
            };

            TunnelPort tunnelPort =
                await tunnelManagementClient.CreateOrUpdateTunnelPortAsync(tunnel, tunnelPortRequest, null, default);

            Console.WriteLine($"Tunnel '{tunnel.TunnelId}' mapped to port {tunnelPort.PortNumber}.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tunnel port: {ex.Message}");

            return 1;
        }
    }

    private static async Task<int> GetAccessTokenAsync(string id, string cluster, IHost host)
    {
        try
        {
            var tunnelRequest = new Tunnel
            {
                TunnelId = id,
                ClusterId = cluster,
                Endpoints = [],
                Ports = []
            };

            var tunnelRequestOptions = new TunnelRequestOptions
            {
                IncludeAccessControl = true,
                IncludePorts = true,
                FollowRedirects = true,
                TokenScopes = [TunnelAccessScopes.Connect],
            };

            TunnelManagementClient tunnelManagementClient = RetrieveTunnelManagementClient(host);

            Tunnel tunnel =
                await tunnelManagementClient.GetTunnelAsync(tunnelRequest, tunnelRequestOptions, default);

            bool retrievedAccessToken = tunnel.TryGetAccessToken(TunnelAccessScopes.Connect, out string accessToken);

            if (retrievedAccessToken)
            {
                Console.WriteLine($"X-Tunnel-Authorization: tunnel {accessToken}");
            }
            else
            {
                Console.WriteLine("Access token not retrieved.");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving access token: {ex.Message}");

            return 1;
        }
    }

    private static async Task<int> ControlAccessAsync(string id, string cluster, bool @public, IHost host)
    {
        string action = @public == true ? "Public" : "Private";

        try
        {
            TunnelAccessControl tunnelAccessControl = @public == true
                ? new TunnelAccessControl
                {
                    Entries =
                    [
                        new TunnelAccessControlEntry
                        {
                            Type = TunnelAccessControlEntryType.Anonymous,
                            Scopes = [TunnelAccessScopes.Connect],
                        }
                    ]
                }
                : new TunnelAccessControl
                {
                    Entries = [],
                };

            var tunnelRequest = new Tunnel
            {
                TunnelId = id,
                ClusterId = cluster,
                Endpoints = [],
                AccessControl = tunnelAccessControl
            };

            var tunnelRequestOptions = new TunnelRequestOptions
            {
                IncludeAccessControl = true,
                IncludePorts = true,
                FollowRedirects = true,
                TokenScopes = [TunnelAccessScopes.Connect],
            };

            TunnelManagementClient tunnelManagementClient = RetrieveTunnelManagementClient(host);

            Tunnel tunnel =
                await tunnelManagementClient.CreateOrUpdateTunnelAsync(tunnelRequest, tunnelRequestOptions, default);

            Console.WriteLine($"{action} access for tunnel {tunnel.TunnelId} has been granted");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error making tunnel {action}: {ex.Message}");

            return 1;
        }
    }

    private static TunnelManagementClient RetrieveTunnelManagementClient(IHost host)
    {
        TunnelAuthorizationProvider tunnelAuthorizationProvider =
                host.Services.GetRequiredService<TunnelAuthorizationProvider>();

        return tunnelAuthorizationProvider.GenerateAuthorizedClient();
    }
}

using Microsoft.DevTunnels.Contracts;
using Microsoft.DevTunnels.Management;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.NamingConventionBinder;
using System.Net.Http.Headers;

namespace AspireDevTunnels.SDK.Commands;

public class Program
{
    public static void Main(string[] args) => BuildCommandLine()
        .UseHost(_ => Host.CreateDefaultBuilder(),
            hostBuilder =>
            {
                hostBuilder.ConfigureAppConfiguration((context, config) =>
                {
                    // Add App Config
                });

                hostBuilder.ConfigureServices((hostBuilderContext, services) =>
                {
                    // Add Services
                });
            })
        .Invoke(args);

    private static CommandLineConfiguration BuildCommandLine()
    {
        var root = new RootCommand(@"$ dotnet run list") { };

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
            new Option<string>("--port", ["-p"]) {
                Required = false
            },
        };
        addPortCommand.Action = CommandHandler.Create<int, IHost>(AddPortAsync);
        root.Subcommands.Add(addPortCommand);

        // Start Dev Tunnel
        var startDevTunnelsCommand = new Command("start", "Start DevTunnels with C# SDK")
        {
            Action = CommandHandler.Create<string, IHost>(StartDevTunnelsAsync)
        };
        root.Subcommands.Add(startDevTunnelsCommand);

        // Show Logged In User
        var showLoggedInUserCommand = new Command("user", "Show user")
        {
            Action = CommandHandler.Create<IHost>(ShowLoggedInUserAsync)
        };
        root.Subcommands.Add(showLoggedInUserCommand);

        return new CommandLineConfiguration(root);
    }

    private static async Task<int> ListDevTunnelsAsync(IHost host)
    {
        try
        {
            var userAgent = new ProductInfoHeaderValue("AspireDevTunnelApp", "1.0");
            var managementClient = new TunnelManagementClient(userAgent, userTokenCallback: null, new Uri("https://tunnels.api.visualstudio.com"));

            // List tunnels to infer the current user (tunnels are tied to the authenticated user)
            Tunnel[] tunnels = await managementClient.ListTunnelsAsync(clusterId: null, domain: null, options: null, ownedTunnelsOnly: true, default);

            Console.WriteLine("Tunnels owned by the current user:");

            foreach (Tunnel tunnel in tunnels)
            {
                Console.WriteLine($"Tunnel ID: {tunnel.TunnelId}, Name: {tunnel.Name}");
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
            var tunnel = new Tunnel
            {
                TunnelId = id,
                Endpoints = [],
                Ports = [],
            };

            var userAgent = new ProductInfoHeaderValue("AspireDevTunnelApp", "1.0");
            var managementClient = new TunnelManagementClient(userAgent, userTokenCallback: null, ManagementApiVersions.Version20230927Preview);
            Tunnel activeTunnel = await managementClient.CreateTunnelAsync(tunnel, null, default);

            Console.WriteLine($"Tunnel '{activeTunnel.Name}' has been created and mapped.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tunnel port: {ex.Message}");

            return 1;
        }
    }

    private static async Task AddPortAsync(int port, IHost host)
    {
        try
        {

            // Set up user agent and authentication callback
            var userAgent = new ProductInfoHeaderValue("MyApp", "1.0");

            static Task<AuthenticationHeaderValue?> tokenCallback()
            {
                // TODO: Implement your token retrieval logic here
                return Task.FromResult(new AuthenticationHeaderValue("Bearer", "<your-access-token>"));
            }

            // Create the management client
            // var managementClient = new TunnelManagementClient(userAgent, tokenCallback);
            var managementClient = new TunnelManagementClient(userAgent, tokenCallback, ManagementApiVersions.Version20230927Preview);

            // Define the tunnel
            var tunnel = new Tunnel
            {
                Name = "my-dev-tunnel",
                // Optionally set Domain, Description, etc.
            };

            // Create the tunnel
            tunnel = await managementClient.CreateTunnelAsync(tunnel, null, default);

            // Define the port mapping
            var tunnelPort = new TunnelPort
            {
                PortNumber = 5000, // The port you want to map
                Protocol = TunnelProtocol.Http, // Or Tcp/Udp as needed
            };

            // Create the port on the tunnel
            await managementClient.CreateTunnelPortAsync(tunnel, tunnelPort, null, default);

            Console.WriteLine($"Tunnel '{tunnel.Name}' created and mapped to port {tunnelPort.PortNumber}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tunnel port: {ex.Message}");
        }
    }

    private static void ShowLoggedInUserAsync(IHost host)
    {
        //try
        //{
        //    var userAgent = new ProductInfoHeaderValue("MyApp", "1.0");
        //    Func<Task<AuthenticationHeaderValue?>> tokenCallback = async () =>
        //    {
        //        // TODO: Implement your token retrieval logic here
        //        return new AuthenticationHeaderValue("Bearer", "<your-access-token>");
        //    };

        //    // Create the management client
        //    var managementClient = new TunnelManagementClient(userAgent, tokenCallback);
        //    var options = new TunnelManagementClientOptions
        //    {
        //        // This will use the default authentication from the environment
        //        // (DevTunnel CLI authentication)
        //    };
        //    // Create a TunnelManagementClient to interact with the DevTunnels service
        //    var tunnelManagementClient = new TunnelManagementClient();

        //    // Get the current user's authentication status
        //    var userInfo = await tunnelManagementClient.GetUserAsync();

        //    if (userInfo != null)
        //    {
        //        Console.WriteLine($"Status: {userInfo.IsAuthenticated}");

        //        if (userInfo.IsAuthenticated)
        //        {
        //            Console.WriteLine($"Username: {userInfo.Username}");
        //            Console.WriteLine($"Provider: {userInfo.Provider}");

        //            // Display additional user properties if available
        //            if (!string.IsNullOrEmpty(userInfo.DisplayName))
        //            {
        //                Console.WriteLine($"Display Name: {userInfo.DisplayName}");
        //            }

        //            if (!string.IsNullOrEmpty(userInfo.Email))
        //            {
        //                Console.WriteLine($"Email: {userInfo.Email}");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("No user is currently logged in. Use 'devtunnel user login' to authenticate.");
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Failed to retrieve user information.");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"Error retrieving user information: {ex.Message}");
        //}
    }

    private static void StartDevTunnelsAsync(string port, IHost host) =>
        Console.WriteLine($"Starting DevTunnels with file path: {port}");
}

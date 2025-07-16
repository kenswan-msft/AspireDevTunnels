using Azure.Core;
using Azure.Identity;
using Microsoft.DevTunnels.Contracts;
using Microsoft.DevTunnels.Management;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;

namespace AspireDevTunnels.Core;

public class MicrosoftDevTunnel
{
    private readonly DevTunnelOptions devTunnelOptions;
    private readonly string scope;
    private readonly string tunnelId;
    private readonly TunnelManagementClient tunnelManagementClient;
    private AccessToken? accessTokenCache;
    private Tunnel? tunnel;
    private string? tunnelClusterId;

    public MicrosoftDevTunnel(string tunnelId, string scope, DevTunnelOptions devTunnelOptions)
    {
        this.tunnelId = tunnelId;
        this.scope = scope;
        this.devTunnelOptions = devTunnelOptions;
        tunnelManagementClient = GenerateAuthorizedClient();
    }

    public async Task<Tunnel> CreateTunnelAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tunnelRequest = new Tunnel { TunnelId = tunnelId };

            tunnel =
                await tunnelManagementClient.CreateOrUpdateTunnelAsync(
                    tunnelRequest,
                    null,
                    cancellationToken);

            tunnelClusterId = tunnel.ClusterId;

            Console.WriteLine($"Tunnel '{tunnel.TunnelId}' has been created on cluster {tunnel.ClusterId}");

            return tunnel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tunnel: {ex.Message}");

            throw;
        }
    }

    public async Task<TunnelPort> AddPortAsync(int port, string uriScheme = TunnelProtocol.Https,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tunnelRequest = new Tunnel
            {
                TunnelId = tunnelId,
                ClusterId = tunnelClusterId
            };

            tunnel =
                await tunnelManagementClient.GetTunnelAsync(tunnelRequest, null, CancellationToken.None);

            // Define the port mapping
            var tunnelPortRequest = new TunnelPort
            {
                PortNumber = (ushort)port,
                Protocol = uriScheme
            };

            TunnelPort tunnelPort =
                await tunnelManagementClient.CreateOrUpdateTunnelPortAsync(
                    tunnel,
                    tunnelPortRequest,
                    null,
                    cancellationToken);

            Console.WriteLine($"Tunnel '{tunnel.TunnelId}' mapped to port {tunnelPort.PortNumber}.");

            return tunnelPort;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tunnel port: {ex.Message}");

            throw;
        }
    }

    public async Task<TunnelPort[]> GetActivePortsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tunnelRequest = new Tunnel
            {
                TunnelId = tunnelId,
                ClusterId = tunnelClusterId
            };

            var tunnelRequestOptions = new TunnelRequestOptions
            {
                IncludeAccessControl = true,
                IncludePorts = true,
                FollowRedirects = true,
                TokenScopes = [TunnelAccessScopes.Connect]
            };

            tunnel =
                await tunnelManagementClient.GetTunnelAsync(
                    tunnelRequest,
                    tunnelRequestOptions,
                    cancellationToken);

            TunnelPort[] tunnelEndpoints = [.. tunnel.Ports];

            Console.WriteLine($"Tunnel '{tunnel.TunnelId}' has been retrieved with {tunnelEndpoints.Length} active ports.");

            return tunnelEndpoints;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving active ports: {ex.Message}");

            throw;
        }
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tunnelRequest = new Tunnel
            {
                TunnelId = tunnelId,
                ClusterId = tunnelClusterId,
                Endpoints = [],
                Ports = []
            };

            var tunnelRequestOptions = new TunnelRequestOptions
            {
                IncludeAccessControl = true,
                IncludePorts = true,
                FollowRedirects = true,
                TokenScopes = [TunnelAccessScopes.Connect]
            };

            tunnel =
                await tunnelManagementClient.GetTunnelAsync(
                    tunnelRequest,
                    tunnelRequestOptions,
                    cancellationToken);

            bool _ =
                tunnel.TryGetAccessToken(TunnelAccessScopes.Connect, out string accessToken);

            return accessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving access token: {ex.Message}");

            throw;
        }
    }

    public async Task<Tunnel> UpdateAccessAsync(bool @public, CancellationToken cancellationToken)
    {
        string action = @public ? "Public" : "Private";

        try
        {
            TunnelAccessControl tunnelAccessControl = @public
                ? new()
                {
                    Entries =
                    [
                        new()
                        {
                            Type = TunnelAccessControlEntryType.Anonymous,
                            Scopes = [TunnelAccessScopes.Connect]
                        }
                    ]
                }
                : new TunnelAccessControl { Entries = [] };

            var tunnelRequest = new Tunnel
            {
                TunnelId = tunnelId,
                ClusterId = tunnelClusterId,
                Endpoints = [],
                AccessControl = tunnelAccessControl
            };

            var tunnelRequestOptions = new TunnelRequestOptions
            {
                IncludeAccessControl = true,
                IncludePorts = true,
                FollowRedirects = true,
                TokenScopes = [TunnelAccessScopes.Connect]
            };

            tunnel =
                await tunnelManagementClient.CreateOrUpdateTunnelAsync(
                    tunnelRequest,
                    tunnelRequestOptions,
                    cancellationToken);

            Console.WriteLine($"{action} access for tunnel {tunnel.TunnelId} has been granted");

            return tunnel;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error making tunnel {action}: {ex.Message}");

            throw;
        }
    }

    private TunnelManagementClient GenerateAuthorizedClient()
    {
        if (string.IsNullOrEmpty(scope))
        {
            throw new(
                "Tunnel scope is not set in the configuration. Please add Authorization:Scope to appsettings.json or appsettings.Development.json. " +
                "Scope can be found by running `devtunnels user show --verbose` under Request Data -> Scope in the generated output");
        }

        return new(
            new ProductInfoHeaderValue("AspireDevTunnelApp", "1.0"),
            async () =>
            {
                if (accessTokenCache is not null)
                {
                    AccessToken accessToken = accessTokenCache.Value;
                    var expirationBuffer = TimeSpan.FromMinutes(5);
                    string? expirationString = accessToken.ExpiresOn.UtcDateTime.ToString("o");

                    var expiration =
                        DateTimeOffset.Parse(expirationString, null, DateTimeStyles.RoundtripKind);

                    if (expiration > DateTimeOffset.UtcNow.Add(expirationBuffer))
                    {
                        return new("Bearer", accessToken.Token);
                    }
                }

                if (devTunnelOptions.HasValidAuthToken)
                {
                    return new("Bearer", devTunnelOptions.AuthToken);
                }

                accessTokenCache = await RefreshAuthorizationTokenAsync();

                return new("Bearer", accessTokenCache.Value.Token);
            },
            ManagementApiVersions.Version20230927Preview);
    }

    private async Task<AccessToken> RefreshAuthorizationTokenAsync()
    {
        var credential = new InteractiveBrowserCredential();
        string[] scopes = [scope];

        AccessToken accessToken =
            await credential.GetTokenAsync(new(scopes));

        await AddTunnelSecretAsync(
            $"{nameof(DevTunnelOptions)}:{nameof(DevTunnelOptions.AuthToken)}",
            accessToken.Token);

        await AddTunnelSecretAsync(
            $"{nameof(DevTunnelOptions)}:{nameof(DevTunnelOptions.AuthTokenExpiration)}",
            accessToken.ExpiresOn.UtcDateTime.ToString("o"));

        return accessToken;
    }

    private static async Task AddTunnelSecretAsync(string key, string value)
    {
        using Process process = new();

        process.StartInfo = new()
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", "user-secrets", "set", key, value, "--id", "2a8148bb-c953-42d9-bd12-0d3ed993a6b3"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new($"Error setting user secret: {error}");
        }

        Console.WriteLine($"User secret set successfully: {output}");
    }
}

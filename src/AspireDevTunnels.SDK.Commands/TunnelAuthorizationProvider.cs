using Azure.Core;
using Azure.Identity;
using Microsoft.DevTunnels.Management;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace AspireDevTunnels.SDK.Commands;

internal class TunnelAuthorizationProvider(IConfiguration configuration)
{
    private const string TUNNEL_AUTH_TOKEN_KEY = "TunnelAuthToken";
    private const string TUNNEL_AUTH_TOKEN_EXPIRATION_KEY = "TunnelAuthTokenExpiration";

    public TunnelManagementClient GenerateAuthorizedClient()
    {
        // Add scope to development json file
        // Found scope by running `devtunnel user show --verbose` in the terminal
        string tunnelScope = configuration.GetValue<string>("Authorization:Scope");
        string savedToken = configuration.GetValue<string>(TUNNEL_AUTH_TOKEN_KEY);
        string savedTokenExpiration = configuration.GetValue<string>(TUNNEL_AUTH_TOKEN_EXPIRATION_KEY);

        return new(
            userAgent: new ProductInfoHeaderValue("AspireDevTunnelApp", "1.0"),
            userTokenCallback: async () =>
            {
                if (!string.IsNullOrEmpty(savedToken) && !string.IsNullOrEmpty(savedTokenExpiration))
                {
                    var expirationBuffer = TimeSpan.FromMinutes(5);
                    var expiration = DateTimeOffset.Parse(savedTokenExpiration, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    if (expiration > DateTimeOffset.UtcNow.Add(expirationBuffer))
                    {
                        return new AuthenticationHeaderValue("Bearer", savedToken);
                    }
                }

                AccessToken accessToken = await RefreshAuthorizationTokenAsync();

                return new AuthenticationHeaderValue("Bearer", accessToken.Token);
            },
            apiVersion: ManagementApiVersions.Version20230927Preview);
    }

    public async Task<AccessToken> RefreshAuthorizationTokenAsync()
    {
        string tunnelScope = configuration.GetValue<string>("Authorization:Scope");
        var credential = new InteractiveBrowserCredential();
        string[] scopes = [tunnelScope];

        AccessToken accessToken =
            await credential.GetTokenAsync(new TokenRequestContext(scopes), default);

        await AddTunnelSecretAsync(TUNNEL_AUTH_TOKEN_KEY, accessToken.Token);
        await AddTunnelSecretAsync(TUNNEL_AUTH_TOKEN_EXPIRATION_KEY, accessToken.ExpiresOn.UtcDateTime.ToString("o"));

        return accessToken;
    }

    private static async Task AddTunnelSecretAsync(string key, string value)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", ["user-secrets", "set", key, value, "--id", "ddd4b748-53b7-4f2f-98b1-b438b6ba4fa5"]),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Error setting user secret: {error}");
        }
        else
        {
            Console.WriteLine($"User secret set successfully: {output}");
        }
    }
}

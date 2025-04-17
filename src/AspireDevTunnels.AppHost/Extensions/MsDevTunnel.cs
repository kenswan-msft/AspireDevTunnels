using System.Diagnostics;
using System.Text.Json;

namespace AspireDevTunnels.AppHost.Extensions
{
    internal class MsDevTunnel : IDevTunnelProvider
    {
        public string Id => tunnelId;
        public string Name => tunnelName;
        public string Url => isInitialized ? GetTunnelUrl() : null;

        private string tunnelId;
        private string tunnelName;
        private int? port;

        private bool isInitialized => !string.IsNullOrEmpty(tunnelId);
        private readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        private record DevTunnelInfo(string TunnelId, string TunnelExpiration, int HostConnections, int ClientConnections);
        private record DevTunnel(DevTunnelInfo Tunnel);
        private record DevTunnelPortInfo(string TunnelId, int PortNumber, string Protocol, int ClientConnections);
        private record DevTunnelPort(DevTunnelPortInfo Port);
        private record DevTunnelAccessInfo(string TunnelId, string Scope, string LifeTime, string Expiration, string Token);
        private record DevTunnelUserInfo(string Status, string Provider, string Username, string TenantId, string ObjectId);

        public async Task CreateTunnelAsync(string tunnelName, CancellationToken cancellationToken = default)
        {
            this.tunnelName = tunnelName;

            await CheckIsDevTunnelsInstalledAsync(cancellationToken);
            await CheckIsUserLoggedInAsync(cancellationToken);

            DevTunnel existingDevTunnel = await CheckIfTunnelExistsAsync(cancellationToken);

            if (existingDevTunnel is not null)
            {
                tunnelId = existingDevTunnel.Tunnel.TunnelId;

                Console.WriteLine($"DevTunnel {tunnelName} already exists. Skipping creation.");

                return;
            }

            List<string> commandLineArgs = [
                "create",
                tunnelName,
                "--json"
            ];

            DevTunnel devTunnel =
                await RunProcessAsync<DevTunnel>(commandLineArgs, cancellationToken);

            if (!string.IsNullOrWhiteSpace(devTunnel.Tunnel?.TunnelId))
            {
                tunnelId = devTunnel.Tunnel.TunnelId;
                Console.WriteLine($"Tunnel Created: {tunnelId}");
                Console.WriteLine($"Tunnel Url: {Url}");
            }
            else
            {
                throw new InvalidOperationException("Failed to create tunnel.");
            }
        }

        public async Task AddPortAsync(int port, string protocol = "https", CancellationToken cancellationToken = default)
        {
            CheckIsInitialized();

            this.port = port;

            bool devTunnelPortExists = await CheckIfDevTunnelPortExistsAsync(port, cancellationToken);

            if (devTunnelPortExists)
            {
                Console.WriteLine($"DevTunnel Port {port} already exists. Skipping addition.");

                return;
            }

            List<string> commandLineArgs = [
                "port",
                "add",
                "-p",
                port.ToString(),
                "--protocol",
                protocol,
                "--json",
            ];

            DevTunnelPort devTunnelPort = await RunProcessAsync<DevTunnelPort>(commandLineArgs, cancellationToken);

            if (devTunnelPort.Port?.PortNumber > 0)
            {
                Console.WriteLine($"Port Added: {devTunnelPort.Port.PortNumber}");
            }
            else
            {
                throw new Exception("Failed to add dev tunnel port.");
            }
        }

        public async Task<string> GetAuthTokenAsync(CancellationToken cancellationToken = default)
        {
            CheckIsInitialized();

            List<string> commandLineArgs = [
                "token",
                tunnelName,
                "--scopes",
                "connect",
                "--json"
            ];

            DevTunnelAccessInfo devTunnelAccessInfo = await RunProcessAsync<DevTunnelAccessInfo>(commandLineArgs, cancellationToken);

            if (!string.IsNullOrWhiteSpace(devTunnelAccessInfo.Token))
            {
                Console.WriteLine($"Token: {devTunnelAccessInfo.Token}");

                return devTunnelAccessInfo.Token;
            }
            else
            {
                throw new Exception("Failed to get access token.");
            }
        }

        public async Task AllowAnonymousAccessAsync(CancellationToken cancellationToken = default)
        {
            CheckIsInitialized();

            List<string> commandLineArgs = [
                "access",
                "create",
                tunnelName,
                "--anonymous"
            ];

            await RunProcessAsync(commandLineArgs, waitForExit: true, collectInputCallback: null, cancellationToken);
        }

        public async Task StartTunnelAsync(CancellationToken cancellationToken = default)
        {
            CheckIsInitialized();

            List<string> commandLineArgs = [
                "host",
            ];

            await RunProcessAsync(commandLineArgs, waitForExit: false, collectInputCallback: null, cancellationToken);
        }

        public Task StopTunnelAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private static async Task CheckIsDevTunnelsInstalledAsync(CancellationToken cancellationToken)
        {
            bool isInstalled = false;

            List<string> commandLineArgs = [
                "--version",
            ];

            await RunProcessAsync(
                commandLineArgs,
                waitForExit: true,
                collectInputCallback: (output) =>
                {
                    if (output.Contains("Tunnel CLI version:"))
                    {
                        // Console.WriteLine(output);
                        isInstalled = true;
                    }
                }, cancellationToken);

            if (!isInstalled)
            {
                throw new InvalidOperationException("DevTunnel CLI is not installed. Please install from https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started.");
            }
        }

        private async Task CheckIsUserLoggedInAsync(CancellationToken cancellationToken)
        {
            List<string> commandLineArgs = [
                "user",
                "show",
                "--json",
            ];

            DevTunnelUserInfo devTunnelUserInfo =
                await RunProcessAsync<DevTunnelUserInfo>(commandLineArgs, cancellationToken);

            if (!string.IsNullOrWhiteSpace(devTunnelUserInfo.Username))
            {
                Console.WriteLine($"Logged In User: {devTunnelUserInfo.Username}");
            }
            else
            {
                throw new InvalidOperationException("User is not logged in. Please log in using 'devtunnel user login'.");
            }
        }

        private async Task<DevTunnel> CheckIfTunnelExistsAsync(CancellationToken cancellationToken)
        {
            List<string> commandLineArgs = [
                "show",
                tunnelName,
                "--json",
            ];

            DevTunnel devTunnel =
                await RunProcessAsync<DevTunnel>(commandLineArgs, cancellationToken);

            if (!string.IsNullOrWhiteSpace(devTunnel?.Tunnel?.TunnelId))
            {
                Console.WriteLine($"Found DevTunnel {tunnelName}: {devTunnel.Tunnel.TunnelId}");

                return devTunnel;
            }
            else
            {
                return default;
            }
        }

        private async Task<bool> CheckIfDevTunnelPortExistsAsync(int portNumber, CancellationToken cancellationToken)
        {
            List<string> commandLineArgs = [
                "port",
                "show",
                tunnelName,
                "--port-number",
                portNumber.ToString(),
                "--json",
            ];

            DevTunnelPort devTunnelPort =
                await RunProcessAsync<DevTunnelPort>(commandLineArgs, cancellationToken);

            if (!string.IsNullOrWhiteSpace(devTunnelPort?.Port?.TunnelId))
            {
                Console.WriteLine($"Found DevTunnel Port {portNumber}: {devTunnelPort.Port.TunnelId}-{devTunnelPort.Port.PortNumber}");

                return true;
            }
            else
            {
                return false;
            }
        }

        private void CheckIsInitialized()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("Tunnel must be initialized before starting. Use CreateTunnelAsync before starting the tunnel.");
            }
        }

        private async Task<T> RunProcessAsync<T>(
            List<string> commandLineArgs,
            CancellationToken cancellationToken = default)
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
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                Console.WriteLine("Error: " + error);
            }

            return process.ExitCode != 0
                ? default
                : !string.IsNullOrWhiteSpace(output) ? JsonSerializer.Deserialize<T>(output, jsonSerializerOptions) : default;
        }

        private static async Task RunProcessAsync(
            List<string> commandLineArgs,
            bool waitForExit = true,
            Action<string> collectInputCallback = null,
            CancellationToken cancellationToken = default)
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
                    if (collectInputCallback is not null)
                    {
                        collectInputCallback(e.Data);
                    }

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

            if (waitForExit)
            {
                await process.WaitForExitAsync(cancellationToken);
            }
        }

        private string GetTunnelUrl()
        {
            return string.IsNullOrWhiteSpace(tunnelId)
                ? throw new ArgumentException("Tunnel ID cannot be null or empty.", nameof(tunnelId))
                : $"https://{tunnelId}.devtunnels.ms";
        }
    }
}

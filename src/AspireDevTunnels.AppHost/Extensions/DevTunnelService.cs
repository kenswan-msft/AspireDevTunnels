using System.Diagnostics;

namespace AspireDevTunnels.AppHost.Extensions
{
    internal class DevTunnelService : IDevTunnelService
    {
        public async Task CreateTunnelAsync(string tunnelName, CancellationToken cancellationToken = default)
        {
            List<string> commandLineArgs = [
                "create",
                tunnelName,
                "--json"
            ];

            await RunProcessAsync(commandLineArgs, waitForExit: true, collectInputCallback: null, cancellationToken);
        }

        public async Task AddPortAsync(int portNumber, string protocol = "https", CancellationToken cancellationToken = default)
        {
            List<string> commandLineArgs = [
                "port",
                "add",
                "-p",
                portNumber.ToString(),
                "--protocol",
                protocol
            ];

            await RunProcessAsync(commandLineArgs, waitForExit: true, collectInputCallback: null, cancellationToken);
        }

        public async Task<string> GetAuthTokenAsync(string tunnelName, CancellationToken cancellationToken = default)
        {
            List<string> commandLineArgs = [
                "token",
                tunnelName,
                "--scopes",
                "connect",
                "--json"
            ];

            string token = string.Empty;

            await RunProcessAsync(commandLineArgs, waitForExit: true, (input) =>
            {
                if (input.Contains("\"token\":"))
                {
                    token = input.Replace("\"token\": \"", string.Empty).Replace("\"", string.Empty);
                }
            }, cancellationToken);

            return token;
        }

        public async Task StartTunnelAsync(CancellationToken cancellationToken = default)
        {
            List<string> commandLineArgs = [
                "host",
            ];

            await RunProcessAsync(commandLineArgs, waitForExit: false, collectInputCallback: null, cancellationToken);
        }

        public Task StopTunnelAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
    }
}

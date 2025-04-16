using System.Diagnostics;

namespace AspireDevTunnels.AppHost.Extensions
{
    internal class DevTunnelService : IDevTunnelService
    {
        public void CreateTunnel(string tunnelName)
        {
            List<string> commandLineArgs = [
                "create",
                tunnelName,
                "--json"
            ];

            RunProcess(commandLineArgs);
        }

        public void AddPort(int portNumber, string protocol = "https")
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

        public string GetAuthToken(string tunnelName)
        {
            List<string> commandLineArgs = [
                "token",
                tunnelName,
                "--scopes",
                "connect",
                "--json"
            ];

            string token = string.Empty;

            RunProcess(commandLineArgs, (input) =>
            {
                if (input.Contains("\"token\":"))
                {
                    token = input.Replace("\"token\": \"", string.Empty).Replace("\"", string.Empty);
                }
            });

            return token;
        }

        public void StartTunnel()
        {
            List<string> commandLineArgs = [
                "host",
            ];

            RunProcess(commandLineArgs);
        }

        public void StopTunnel()
        {
            //List<string> commandLineArgs = [
            //    "stop",
            //];

            //RunProcess(commandLineArgs);
        }

        private static void RunProcess(List<string> commandLineArgs, Action<string> collectInputCallback = null)
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
            process.WaitForExit(milliseconds: 5000);
        }
    }
}

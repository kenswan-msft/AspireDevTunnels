namespace AspireDevTunnels.Core.Commands;

public class DevTunnelCommandResult(string output, string error, int exitCode)
{
    public string Output { get; init; } = output;

    public string Error { get; init; } = error;

    public int ExitCode { get; init; } = exitCode;

    public bool IsSuccess => ExitCode == 0;
}

public class DevTunnelCommandResult<T>(T value, string output, string error, int exitCode)
    : DevTunnelCommandResult(output, error, exitCode)
{
    public T Value { get; init; } = value;
}

# AspireDevTunnels

Prototype for .NET Aspire DevTunnel Feature

## Dependencies

- [.NET Aspire](https://github.com/dotnet/aspire) (9.2.0)
- [DevTunnels CLI](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/cli-commands) ([install](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows))

## Resources

- [Microsoft/Dev-Tunnels GitHub](https://github.com/microsoft/dev-tunnels)
- [Microsoft Build - Advanced Dev Tunnel Features](https://www.youtube.com/watch?v=yCYLurylgj8)

## DevTunnel Startup Steps

### 1. Login

`devtunnel user login`

### 2. Create Port

`devtunnel create`

### 3. Add Port

`devtunnel port add -p 7565 --protocol https`

### 4. Startup

`devtunnel host`

## Issues

1. Get Consistent Dev Tunnel Url through Aspire Pipeline/builder configuration
   1. Could surface this URL through dashboard if unable to make consistent
1. Configure port connection through Aspire Pipeline/builder configuration
1. Logging in and requesting through browser works, but missing for:
   1. Visual Studio http files
   1. Rest Client IDEs

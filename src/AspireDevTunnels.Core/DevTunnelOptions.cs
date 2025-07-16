using System.Globalization;

namespace AspireDevTunnels.Core;

public class DevTunnelOptions
{
    public string AuthToken { get; set; }

    public string AuthTokenExpiration { get; set; }

    public int ExpirationBufferMinutes { get; set; } = 5;

    public string Scope { get; set; }

    public bool HasValidAuthToken
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AuthToken) || string.IsNullOrWhiteSpace(AuthTokenExpiration))
            {
                return false;
            }

            var expirationBuffer = TimeSpan.FromMinutes(ExpirationBufferMinutes);

            var expiration =
                DateTimeOffset.Parse(AuthTokenExpiration, null, DateTimeStyles.RoundtripKind);

            return expiration > DateTimeOffset.UtcNow.Add(expirationBuffer);
        }
    }
}

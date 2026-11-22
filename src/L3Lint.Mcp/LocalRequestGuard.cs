using System.Security.Cryptography;
using System.Text;

namespace L3Lint.Mcp;

/// <summary>
/// Protects the local HTTP endpoint from web pages (DNS rebinding, Architecture tab threat table): the Host header
/// must name the loopback address, a browser Origin must be localhost or explicitly allowed, and when a token is
/// set every request must carry it as a bearer token.
/// </summary>
internal sealed class LocalRequestGuard(RequestDelegate next, ServerSettings settings)
{
    private static readonly string[] LoopbackHosts = ["localhost", "127.0.0.1", "[::1]"];

    private readonly byte[]? _token = settings.Token is { } token ? Encoding.UTF8.GetBytes("Bearer " + token) : null;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!LoopbackHosts.Contains(context.Request.Host.Host, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(Resources.Strings.HttpHostRejected).ConfigureAwait(false);
            return;
        }

        if (context.Request.Headers.Origin is { Count: > 0 } origins && !IsAllowed(origins.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(Resources.Strings.HttpOriginRejected).ConfigureAwait(false);
            return;
        }

        if (_token is not null && !HasToken(context.Request.Headers.Authorization.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsync(Resources.Strings.HttpTokenRequired).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private bool IsAllowed(string origin)
    {
        if (settings.AllowedOrigins.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase));
    }

    // Constant-time comparison, so response timing reveals nothing about the token.
    private bool HasToken(string header) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(header), _token);
}

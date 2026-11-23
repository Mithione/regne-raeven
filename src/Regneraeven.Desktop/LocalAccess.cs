using System.Security.Cryptography;
using System.Text;

namespace Regneraeven.Desktop;

/// <summary>
/// The server only listens on 127.0.0.1, but other programs (or web pages in a normal browser) could still
/// call it. Every launch gets a random key: the window opens /?k=KEY once, which sets a cookie, and
/// requests without that cookie are refused. A future website replaces this with a real login.
/// </summary>
public class LocalAccess
{
    public string Token { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
    public const string CookieName = "rr.session";

    public bool Matches(string? value) =>
        value is not null && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(value), Encoding.ASCII.GetBytes(Token));
}

public class LocalAccessMiddleware(RequestDelegate next, LocalAccess access)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        // Refuse anything that isn't addressed to this machine (DNS-rebinding protection).
        var host = ctx.Request.Host.Host;
        if (host is not ("127.0.0.1" or "localhost" or "[::1]" or "::1"))
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (ctx.Request.Query.TryGetValue("k", out var k) && access.Matches(k))
        {
            ctx.Response.Cookies.Append(LocalAccess.CookieName, access.Token, new CookieOptions
            {
                HttpOnly = true, SameSite = SameSiteMode.Strict, IsEssential = true, Path = "/",
            });
            ctx.Response.Redirect(ctx.Request.Path.HasValue && ctx.Request.Path != "/" ? ctx.Request.Path.Value! : "/");
            return;
        }

        if (!access.Matches(ctx.Request.Cookies[LocalAccess.CookieName]))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync("<!doctype html><meta charset=utf-8><title>Regneræven</title>" +
                "<body style=\"font-family:system-ui;padding:48px;color:#344054\"><h2>Regneræven</h2>" +
                "<p>Regneræven kan kun åbnes fra programmet Regneræven.</p></body>");
            return;
        }

        await next(ctx);
    }
}

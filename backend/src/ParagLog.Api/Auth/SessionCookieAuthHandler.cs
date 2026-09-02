using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ParagLog.Core.Users;

namespace ParagLog.Api.Auth;

public static class SessionCookieDefaults
{
    public const string Scheme = "SessionCookie";
    public const string CookieName = "paraglog_session";
}

public sealed class SessionCookieAuthOptions : AuthenticationSchemeOptions;

public sealed class SessionCookieAuthHandler(
    IOptionsMonitor<SessionCookieAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    UserService userService)
    : AuthenticationHandler<SessionCookieAuthOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SessionCookieDefaults.CookieName, out var token) || string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        var result = await userService.ValidateSessionAsync(token, Context.RequestAborted);
        if (!result.IsSuccess)
            return AuthenticateResult.Fail("Invalid or expired session.");

        var (user, sessionId) = result.Value;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("sid", sessionId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}

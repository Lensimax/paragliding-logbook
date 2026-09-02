using Microsoft.AspNetCore.Mvc;
using ParagLog.Api.Auth;
using ParagLog.Api.Common;
using ParagLog.Api.Contracts.Auth;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Users;

namespace ParagLog.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();

        app.MapGet("/api/me", Me).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, [FromServices] UserService userService, HttpResponse response, CancellationToken ct)
    {
        var result = await userService.RegisterAsync(
            request.Username, request.Email, request.Password, request.PasswordConfirmation, ct);

        if (!result.IsSuccess)
            return result.ToProblem();

        SetSessionCookie(response, result.Value.Token, result.Value.ExpiresAt);
        return Results.Ok(UserResponse.From(result.Value.User));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, [FromServices] UserService userService, HttpResponse response, CancellationToken ct)
    {
        var result = await userService.LoginAsync(request.Email, request.Password, request.StayConnected, ct);

        if (!result.IsSuccess)
            return result.ToProblem();

        SetSessionCookie(response, result.Value.Token, result.Value.ExpiresAt);
        return Results.Ok(UserResponse.From(result.Value.User));
    }

    private static async Task<IResult> LogoutAsync(
        ICurrentUser currentUser, [FromServices] UserService userService, HttpResponse response, CancellationToken ct)
    {
        await userService.LogoutAsync(currentUser.Id, currentUser.SessionId, ct);
        response.Cookies.Delete(SessionCookieDefaults.CookieName);
        return Results.NoContent();
    }

    private static async Task<IResult> Me(ICurrentUser currentUser, IUserRepository users, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(currentUser.Id, ct);
        return user is null ? Results.Unauthorized() : Results.Ok(UserResponse.From(user));
    }

    private static void SetSessionCookie(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(SessionCookieDefaults.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/",
        });
}

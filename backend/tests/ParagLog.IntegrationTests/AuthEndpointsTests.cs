using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ParagLog.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_then_me_then_logout_round_trips()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"user{suffix}",
            Email = $"user{suffix}@example.com",
            Password = "a-very-secure-passphrase",
            PasswordConfirmation = "a-very-secure-passphrase",
        });

        registerResponse.EnsureSuccessStatusCode();
        var cookie = ExtractSessionCookie(registerResponse);

        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        meRequest.Headers.Add("Cookie", cookie);
        var meResponse = await client.SendAsync(meRequest);
        meResponse.EnsureSuccessStatusCode();

        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", cookie);
        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var meAfterLogoutRequest = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        meAfterLogoutRequest.Headers.Add("Cookie", cookie);
        var meAfterLogoutResponse = await client.SendAsync(meAfterLogoutRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_duplicate_username()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            Username = $"dup{suffix}",
            Email = $"dup{suffix}@example.com",
            Password = "a-very-secure-passphrase",
            PasswordConfirmation = "a-very-secure-passphrase",
        };

        var first = await client.PostAsJsonAsync("/api/auth/register", payload);
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register", payload with { Email = $"other{suffix}@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_wrong_password()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"login{suffix}",
            Email = $"login{suffix}@example.com",
            Password = "a-very-secure-passphrase",
            PasswordConfirmation = "a-very-secure-passphrase",
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = $"login{suffix}@example.com",
            Password = "the-wrong-passphrase",
            StayConnected = false,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie").First(h => h.StartsWith("paraglog_session="));
        return setCookie[..setCookie.IndexOf(';')];
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ParagLog.IntegrationTests;

public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Repeated_login_attempts_are_throttled_with_429()
    {
        var client = _factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 25; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new
            {
                Email = "nobody@example.com",
                Password = "wrong-password",
                StayConnected = false,
            });

            if (last.StatusCode == HttpStatusCode.TooManyRequests)
                break;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }
}

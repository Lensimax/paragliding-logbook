using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ParagLog.IntegrationTests;

public class ActivityEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ActivityEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"activity{suffix}",
            Email = $"activity{suffix}@example.com",
            Password = "a-very-secure-passphrase",
            PasswordConfirmation = "a-very-secure-passphrase",
        });
        registerResponse.EnsureSuccessStatusCode();

        var setCookie = registerResponse.Headers.GetValues("Set-Cookie").First(h => h.StartsWith("paraglog_session="));
        client.DefaultRequestHeaders.Add("Cookie", setCookie[..setCookie.IndexOf(';')]);
        return client;
    }

    [Fact]
    public async Task Create_then_get_round_trips_a_flight()
    {
        var client = await CreateAuthenticatedClientAsync();
        var startedAt = DateTimeOffset.UtcNow.AddDays(-1);

        var createResponse = await client.PostAsJsonAsync("/api/activities", new
        {
            Type = "flight",
            Name = "Evening glide",
            StartedAt = startedAt,
            EndedAt = startedAt.AddHours(1),
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
            LocalTz = "Europe/Paris",
            TakeoffLocation = "Col de la Forclaz",
            LandingLocation = "Doussard",
            Comment = "Smooth thermals",
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        Assert.Equal("flight", created.GetProperty("type").GetString());
        Assert.Equal(3600, created.GetProperty("durationSeconds").GetInt32());

        var getResponse = await client.GetAsync($"/api/activities/{id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Evening glide", fetched.GetProperty("name").GetString());
        Assert.Equal("Col de la Forclaz", fetched.GetProperty("takeoffLocation").GetString());
    }

    [Fact]
    public async Task Create_ground_handling_with_wind_fields()
    {
        var client = await CreateAuthenticatedClientAsync();
        var startedAt = DateTimeOffset.UtcNow.AddHours(-2);

        var createResponse = await client.PostAsJsonAsync("/api/activities", new
        {
            Type = "groundHandling",
            Name = "Beach session",
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
            WindSpeedKmh = 15,
            WindDirection = 270,
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("groundHandling", created.GetProperty("type").GetString());
        Assert.Equal(270, created.GetProperty("windDirection").GetInt32());
    }

    [Fact]
    public async Task Update_then_delete_round_trips()
    {
        var client = await CreateAuthenticatedClientAsync();
        var startedAt = DateTimeOffset.UtcNow.AddDays(-2);

        var createResponse = await client.PostAsJsonAsync("/api/activities", new
        {
            Type = "flight",
            Name = "Original name",
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/activities/{id}", new
        {
            Type = "flight",
            Name = "Renamed flight",
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
        });
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Renamed flight", updated.GetProperty("name").GetString());

        var deleteResponse = await client.DeleteAsync($"/api/activities/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await client.GetAsync($"/api/activities/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task List_returns_most_recent_first_and_paginates()
    {
        var client = await CreateAuthenticatedClientAsync();
        var baseTime = DateTimeOffset.UtcNow.AddDays(-30);

        for (var i = 0; i < 3; i++)
        {
            var startedAt = baseTime.AddDays(i);
            var response = await client.PostAsJsonAsync("/api/activities", new
            {
                Type = "flight",
                Name = $"Flight {i}",
                StartedAt = startedAt,
                LocalDate = DateOnly.FromDateTime(startedAt.Date),
            });
            response.EnsureSuccessStatusCode();
        }

        var firstPage = await client.GetFromJsonAsync<JsonElement>("/api/activities?limit=2");
        var items = firstPage.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Flight 2", items[0].GetProperty("name").GetString());
        Assert.Equal("Flight 1", items[1].GetProperty("name").GetString());

        var nextCursor = firstPage.GetProperty("nextCursor").GetString();
        Assert.NotNull(nextCursor);

        var secondPage = await client.GetFromJsonAsync<JsonElement>($"/api/activities?limit=2&cursor={nextCursor}");
        var secondItems = secondPage.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(secondItems);
        Assert.Equal("Flight 0", secondItems[0].GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, secondPage.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public async Task Create_rejects_end_before_start()
    {
        var client = await CreateAuthenticatedClientAsync();
        var startedAt = DateTimeOffset.UtcNow;

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            Type = "flight",
            Name = "Time travel",
            StartedAt = startedAt,
            EndedAt = startedAt.AddHours(-1),
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activities_are_isolated_per_user()
    {
        var owner = await CreateAuthenticatedClientAsync();
        var stranger = await CreateAuthenticatedClientAsync();
        var startedAt = DateTimeOffset.UtcNow;

        var createResponse = await owner.PostAsJsonAsync("/api/activities", new
        {
            Type = "flight",
            Name = "Private flight",
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var getResponse = await stranger.GetAsync($"/api/activities/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

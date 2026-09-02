using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ParagLog.IntegrationTests;

public class EquipmentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EquipmentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"equip{suffix}",
            Email = $"equip{suffix}@example.com",
            Password = "a-very-secure-passphrase",
            PasswordConfirmation = "a-very-secure-passphrase",
        });
        registerResponse.EnsureSuccessStatusCode();

        var setCookie = registerResponse.Headers.GetValues("Set-Cookie").First(h => h.StartsWith("paraglog_session="));
        client.DefaultRequestHeaders.Add("Cookie", setCookie[..setCookie.IndexOf(';')]);
        return client;
    }

    [Fact]
    public async Task Create_then_get_round_trips_with_revisions_and_usage()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/equipment", new
        {
            DisplayName = "Ozone Rush 6",
            Type = "wing",
            Brand = "Ozone",
            Model = "Rush 6",
            PurchaseDate = "2024-01-15",
            NextRevisionDate = "2027-01-15",
            AutoAdd = true,
            Revisions = new[] { new { RevisionDate = "2025-01-15", Comment = "Annual check" } },
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("wing", created.GetProperty("type").GetString());
        Assert.True(created.GetProperty("autoAdd").GetBoolean());

        var getResponse = await client.GetAsync($"/api/equipment/{id}");
        getResponse.EnsureSuccessStatusCode();
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ozone Rush 6", fetched.GetProperty("displayName").GetString());
        Assert.Single(fetched.GetProperty("revisions").EnumerateArray());
        Assert.Equal(0, fetched.GetProperty("usage").GetProperty("activityCount").GetInt32());
    }

    [Fact]
    public async Task List_is_ordered_by_creation_date()
    {
        var client = await CreateAuthenticatedClientAsync();

        foreach (var name in new[] { "First", "Second", "Third" })
        {
            var response = await client.PostAsJsonAsync("/api/equipment", new
            {
                DisplayName = name,
                Type = "harness",
                AutoAdd = false,
            });
            response.EnsureSuccessStatusCode();
        }

        var list = await client.GetFromJsonAsync<JsonElement>("/api/equipment");
        var names = list.EnumerateArray().Select(e => e.GetProperty("displayName").GetString()).ToList();

        Assert.Equal(["First", "Second", "Third"], names);
    }

    [Fact]
    public async Task Create_rejects_duplicate_display_name()
    {
        var client = await CreateAuthenticatedClientAsync();
        var payload = new { DisplayName = "Duplicate wing", Type = "wing", AutoAdd = false };

        var first = await client.PostAsJsonAsync("/api/equipment", payload);
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/equipment", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Delete_hard_deletes_unused_equipment()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/equipment", new
        {
            DisplayName = "Unused reserve",
            Type = "reserve",
            AutoAdd = false,
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var deleteResponse = await client.DeleteAsync($"/api/equipment/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await client.GetAsync($"/api/equipment/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Delete_is_refused_and_retire_succeeds_when_equipment_is_in_use()
    {
        var client = await CreateAuthenticatedClientAsync();

        var equipmentResponse = await client.PostAsJsonAsync("/api/equipment", new
        {
            DisplayName = "In-use harness",
            Type = "harness",
            AutoAdd = false,
        });
        equipmentResponse.EnsureSuccessStatusCode();
        var equipment = await equipmentResponse.Content.ReadFromJsonAsync<JsonElement>();
        var equipmentId = equipment.GetProperty("id").GetGuid();

        var startedAt = DateTimeOffset.UtcNow;
        var activityResponse = await client.PostAsJsonAsync("/api/activities", new
        {
            Type = "flight",
            Name = "Flight with harness",
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
            EquipmentIds = new[] { equipmentId },
        });
        Assert.Equal(HttpStatusCode.Created, activityResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/equipment/{equipmentId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var retireResponse = await client.PostAsync($"/api/equipment/{equipmentId}/retire", null);
        retireResponse.EnsureSuccessStatusCode();
        var retired = await retireResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(retired.GetProperty("retired").GetBoolean());
    }

    [Fact]
    public async Task Create_activity_rejects_equipment_owned_by_another_user()
    {
        var owner = await CreateAuthenticatedClientAsync();
        var stranger = await CreateAuthenticatedClientAsync();

        var equipmentResponse = await owner.PostAsJsonAsync("/api/equipment", new
        {
            DisplayName = "Owner's wing",
            Type = "wing",
            AutoAdd = false,
        });
        equipmentResponse.EnsureSuccessStatusCode();
        var equipment = await equipmentResponse.Content.ReadFromJsonAsync<JsonElement>();
        var equipmentId = equipment.GetProperty("id").GetGuid();

        var startedAt = DateTimeOffset.UtcNow;
        var activityResponse = await stranger.PostAsJsonAsync("/api/activities", new
        {
            Type = "flight",
            Name = "Borrowed wing flight",
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
            EquipmentIds = new[] { equipmentId },
        });

        Assert.Equal(HttpStatusCode.BadRequest, activityResponse.StatusCode);
    }
}

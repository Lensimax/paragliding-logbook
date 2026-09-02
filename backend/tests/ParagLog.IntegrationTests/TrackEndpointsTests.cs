using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ParagLog.IntegrationTests;

public class TrackEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string SampleGpx =
        "<?xml version=\"1.0\"?><gpx version=\"1.1\"><trk><trkseg>" +
        "<trkpt lat=\"45.86\" lon=\"6.29\"><ele>1000</ele><time>2026-03-05T09:00:00Z</time></trkpt>" +
        "<trkpt lat=\"45.87\" lon=\"6.30\"><ele>1100</ele><time>2026-03-05T09:05:00Z</time></trkpt>" +
        "</trkseg></trk></gpx>";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _blobsRoot;

    public TrackEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _blobsRoot = Path.Combine(Path.GetTempPath(), "paraglog-test-blobs-" + Guid.NewGuid().ToString("N"));
        _factory = factory.WithWebHostBuilder(builder => builder.UseSetting("Storage:BlobsRoot", _blobsRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(_blobsRoot))
            Directory.Delete(_blobsRoot, recursive: true);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"track{suffix}",
            Email = $"track{suffix}@example.com",
            Password = "a-very-secure-passphrase",
            PasswordConfirmation = "a-very-secure-passphrase",
        });
        registerResponse.EnsureSuccessStatusCode();

        var setCookie = registerResponse.Headers.GetValues("Set-Cookie").First(h => h.StartsWith("paraglog_session="));
        client.DefaultRequestHeaders.Add("Cookie", setCookie[..setCookie.IndexOf(';')]);
        return client;
    }

    private static async Task<Guid> CreateActivityAsync(HttpClient client, string type = "flight")
    {
        var startedAt = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            Type = type,
            Name = "Track test activity",
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent BuildUpload(string fileName, string content, string mediaType = "application/gpx+xml")
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        form.Add(fileContent, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Upload_then_download_round_trips_the_exact_bytes()
    {
        var client = await CreateAuthenticatedClientAsync();
        var activityId = await CreateActivityAsync(client);

        using var uploadContent = BuildUpload("flight.gpx", SampleGpx);
        var uploadResponse = await client.PostAsync($"/api/activities/{activityId}/track", uploadContent);
        uploadResponse.EnsureSuccessStatusCode();
        var updated = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("track.gpx", updated.GetProperty("track").GetProperty("filename").GetString());
        Assert.Equal("gpx", updated.GetProperty("track").GetProperty("format").GetString());

        var downloadResponse = await client.GetAsync($"/api/activities/{activityId}/track");
        downloadResponse.EnsureSuccessStatusCode();
        var downloaded = await downloadResponse.Content.ReadAsStringAsync();
        Assert.Equal(SampleGpx, downloaded);
        Assert.Equal("application/gpx+xml", downloadResponse.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(downloadResponse.Headers.ETag);
    }

    [Fact]
    public async Task Upload_replaces_an_existing_track()
    {
        var client = await CreateAuthenticatedClientAsync();
        var activityId = await CreateActivityAsync(client);

        using (var first = BuildUpload("flight.gpx", SampleGpx))
            (await client.PostAsync($"/api/activities/{activityId}/track", first)).EnsureSuccessStatusCode();

        const string secondGpx =
            "<?xml version=\"1.0\"?><gpx version=\"1.1\"><trk><trkseg>" +
            "<trkpt lat=\"46.0\" lon=\"6.5\"><ele>1200</ele><time>2026-03-06T09:00:00Z</time></trkpt>" +
            "</trkseg></trk></gpx>";
        using (var second = BuildUpload("flight2.gpx", secondGpx))
            (await client.PostAsync($"/api/activities/{activityId}/track", second)).EnsureSuccessStatusCode();

        var downloadResponse = await client.GetAsync($"/api/activities/{activityId}/track");
        var downloaded = await downloadResponse.Content.ReadAsStringAsync();
        Assert.Equal(secondGpx, downloaded);
    }

    [Fact]
    public async Task Upload_rejects_an_unsupported_extension()
    {
        var client = await CreateAuthenticatedClientAsync();
        var activityId = await CreateActivityAsync(client);

        using var content = BuildUpload("flight.txt", "not a track", "text/plain");
        var response = await client.PostAsync($"/api/activities/{activityId}/track", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_rejects_ground_handling_activities()
    {
        var client = await CreateAuthenticatedClientAsync();
        var activityId = await CreateActivityAsync(client, type: "groundHandling");

        using var content = BuildUpload("flight.gpx", SampleGpx);
        var response = await client.PostAsync($"/api/activities/{activityId}/track", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_track_removes_it_but_keeps_the_activity()
    {
        var client = await CreateAuthenticatedClientAsync();
        var activityId = await CreateActivityAsync(client);

        using (var upload = BuildUpload("flight.gpx", SampleGpx))
            (await client.PostAsync($"/api/activities/{activityId}/track", upload)).EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/activities/{activityId}/track");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var downloadResponse = await client.GetAsync($"/api/activities/{activityId}/track");
        Assert.Equal(HttpStatusCode.NotFound, downloadResponse.StatusCode);

        var activityResponse = await client.GetAsync($"/api/activities/{activityId}");
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, activity.GetProperty("track").ValueKind);
    }

    [Fact]
    public async Task Deleting_the_activity_removes_its_blob_folder_from_disk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var activityId = await CreateActivityAsync(client);

        using (var upload = BuildUpload("flight.gpx", SampleGpx))
            (await client.PostAsync($"/api/activities/{activityId}/track", upload)).EnsureSuccessStatusCode();

        var activityFolder = Directory.GetDirectories(_blobsRoot, "*", SearchOption.AllDirectories)
            .First(d => d.EndsWith(activityId.ToString()));
        Assert.True(Directory.Exists(activityFolder));

        var deleteResponse = await client.DeleteAsync($"/api/activities/{activityId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.False(Directory.Exists(activityFolder));
    }
}

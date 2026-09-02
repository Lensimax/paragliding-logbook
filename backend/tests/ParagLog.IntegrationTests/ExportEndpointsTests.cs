using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ParagLog.Core.Abstractions;

namespace ParagLog.IntegrationTests;

public class ExportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string SampleGpx =
        "<?xml version=\"1.0\"?><gpx version=\"1.1\"><trk><trkseg>" +
        "<trkpt lat=\"45.86\" lon=\"6.29\"><ele>1000</ele><time>2026-03-05T09:00:00Z</time></trkpt>" +
        "<trkpt lat=\"45.87\" lon=\"6.30\"><ele>1100</ele><time>2026-03-05T09:05:00Z</time></trkpt>" +
        "</trkseg></trk></gpx>";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _blobsRoot;

    public ExportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _blobsRoot = Path.Combine(Path.GetTempPath(), "paraglog-test-blobs-" + Guid.NewGuid().ToString("N"));
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Storage:BlobsRoot", _blobsRoot);
            builder.ConfigureServices(services =>
                services.AddSingleton<IElevationService>(new FakeElevationService(1234.5)));
        });
    }

    private sealed class FakeElevationService(double elevation) : IElevationService
    {
        public Task<IReadOnlyList<double?>> GetElevationsAsync(
            IReadOnlyList<(double Lat, double Lon)> points, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<double?>>(points.Select(_ => (double?)elevation).ToList());
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
            Username = $"export{suffix}",
            Email = $"export{suffix}@example.com",
            Password = "a-very-secure-passphrase",
            PasswordConfirmation = "a-very-secure-passphrase",
        });
        registerResponse.EnsureSuccessStatusCode();

        var setCookie = registerResponse.Headers.GetValues("Set-Cookie").First(h => h.StartsWith("paraglog_session="));
        client.DefaultRequestHeaders.Add("Cookie", setCookie[..setCookie.IndexOf(';')]);
        return client;
    }

    private static async Task<Guid> CreateActivityAsync(HttpClient client, string name, string type = "flight")
    {
        var startedAt = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            Type = type,
            Name = name,
            StartedAt = startedAt,
            LocalDate = DateOnly.FromDateTime(startedAt.Date),
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Export_zip_contains_csvs_and_the_uploaded_track()
    {
        var client = await CreateAuthenticatedClientAsync();

        var equipmentResponse = await client.PostAsJsonAsync("/api/equipment", new
        {
            DisplayName = "My Wing",
            Type = "wing",
            AutoAdd = false,
        });
        equipmentResponse.EnsureSuccessStatusCode();
        var equipmentId = (await equipmentResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var flightId = await CreateActivityAsync(client, "Export flight");
        var groundHandlingId = await CreateActivityAsync(client, "Export ground handling", type: "groundHandling");

        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleGpx));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/gpx+xml");
        form.Add(fileContent, "file", "flight.gpx");
        (await client.PostAsync($"/api/activities/{flightId}/track", form)).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/export");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        await using var zipStream = await response.Content.ReadAsStreamAsync();
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var activitiesCsv = await ReadEntryAsync(zip, "activities.csv");
        Assert.Contains("Export flight", activitiesCsv);
        Assert.Contains("Export ground handling", activitiesCsv);

        var equipmentCsv = await ReadEntryAsync(zip, "equipment.csv");
        Assert.Contains("My Wing", equipmentCsv);

        var trackEntry = zip.GetEntry($"tracks/{flightId}.gpx");
        Assert.NotNull(trackEntry);

        Assert.Null(zip.GetEntry($"tracks/{groundHandlingId}.gpx"));

        var _ = equipmentId; // exercised via the CSV assertion above
    }

    private static async Task<string> ReadEntryAsync(ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name);
        Assert.NotNull(entry);
        await using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

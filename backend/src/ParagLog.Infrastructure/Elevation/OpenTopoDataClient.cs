using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ParagLog.Core.Abstractions;

namespace ParagLog.Infrastructure.Elevation;

/// <summary>
/// Batch ground-elevation lookup against OpenTopoData (self-hostable, or the public instance).
/// Best-effort: any failure (network, rate limit, bad response) logs and returns nulls rather
/// than throwing, since elevation is an enhancement - it must never fail a track upload.
/// </summary>
public sealed class OpenTopoDataClient(HttpClient httpClient, ILogger<OpenTopoDataClient> logger) : IElevationService
{
    private const int BatchSize = 100; // OpenTopoData's public-instance limit per request
    private const string Dataset = "srtm90m";
    private const int MaxAttempts = 3;
    private static readonly TimeSpan BatchGap = TimeSpan.FromSeconds(1); // public instance is rate-limited to ~1 req/s
    private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(1);

    public async Task<IReadOnlyList<double?>> GetElevationsAsync(
        IReadOnlyList<(double Lat, double Lon)> points, CancellationToken ct)
    {
        var results = new double?[points.Count];

        for (var offset = 0; offset < points.Count; offset += BatchSize)
        {
            if (offset > 0)
                await Task.Delay(BatchGap, ct);

            var batch = points.Skip(offset).Take(BatchSize).ToList();
            var batchResults = await GetBatchWithRetryAsync(batch, ct);
            if (batchResults is null)
                continue; // leave this batch's slots null; the rest may still succeed

            for (var i = 0; i < batchResults.Count && offset + i < results.Length; i++)
                results[offset + i] = batchResults[i];
        }

        return results;
    }

    // A batch that fails transiently (rate limit, timeout, transient network error) is retried
    // with backoff rather than left null outright - without this, a single throttled request on
    // a multi-batch track permanently loses ground elevation for that whole quarter/fifth of the
    // flight, since elevation.json is resolved once on upload and cached forever after.
    private async Task<IReadOnlyList<double?>?> GetBatchWithRetryAsync(
        IReadOnlyList<(double Lat, double Lon)> batch, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var (result, transient) = await GetBatchAsync(batch, ct);
            if (result is not null || !transient || attempt == MaxAttempts)
                return result;

            await Task.Delay(BaseRetryDelay * attempt, ct);
        }

        return null;
    }

    private async Task<(IReadOnlyList<double?>? Result, bool Transient)> GetBatchAsync(
        IReadOnlyList<(double Lat, double Lon)> batch, CancellationToken ct)
    {
        try
        {
            var locations = string.Join('|', batch.Select(p =>
                $"{p.Lat.ToString(CultureInfo.InvariantCulture)},{p.Lon.ToString(CultureInfo.InvariantCulture)}"));

            using var response = await httpClient.GetAsync($"/v1/{Dataset}?locations={locations}", ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Elevation lookup failed with status {Status}", response.StatusCode);
                // 429 (rate limit) and 5xx are worth retrying; other 4xx (bad request) won't
                // succeed on retry.
                var transient = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || (int)response.StatusCode >= 500;
                return (null, transient);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!document.RootElement.TryGetProperty("results", out var resultsElement))
                return (null, false);

            var elevations = new List<double?>(batch.Count);
            foreach (var result in resultsElement.EnumerateArray())
            {
                elevations.Add(
                    result.TryGetProperty("elevation", out var elevation) && elevation.ValueKind == JsonValueKind.Number
                        ? elevation.GetDouble()
                        : null);
            }

            return (elevations, false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Elevation lookup failed");
            return (null, true);
        }
    }
}

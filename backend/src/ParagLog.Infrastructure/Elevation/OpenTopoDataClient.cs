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

    public async Task<IReadOnlyList<double?>> GetElevationsAsync(
        IReadOnlyList<(double Lat, double Lon)> points, CancellationToken ct)
    {
        var results = new double?[points.Count];

        for (var offset = 0; offset < points.Count; offset += BatchSize)
        {
            var batch = points.Skip(offset).Take(BatchSize).ToList();
            var batchResults = await GetBatchAsync(batch, ct);
            if (batchResults is null)
                continue; // leave this batch's slots null; the rest may still succeed

            for (var i = 0; i < batchResults.Count && offset + i < results.Length; i++)
                results[offset + i] = batchResults[i];
        }

        return results;
    }

    private async Task<IReadOnlyList<double?>?> GetBatchAsync(
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
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!document.RootElement.TryGetProperty("results", out var resultsElement))
                return null;

            var elevations = new List<double?>(batch.Count);
            foreach (var result in resultsElement.EnumerateArray())
            {
                elevations.Add(
                    result.TryGetProperty("elevation", out var elevation) && elevation.ValueKind == JsonValueKind.Number
                        ? elevation.GetDouble()
                        : null);
            }

            return elevations;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Elevation lookup failed");
            return null;
        }
    }
}

using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;

namespace ParagLog.Core.Export;

/// <summary>
/// Streams the whole logbook as a ZIP: activities.csv, equipment.csv, tracks/{id}.{gpx|igc}.
/// Written directly to the caller's stream (the HTTP response body) so memory stays flat -
/// track bytes are copied straight from the blob store into the zip entry, never buffered whole.
/// </summary>
public sealed class ExportService(IActivityRepository activities, IEquipmentRepository equipment, IBlobStore blobStore)
{
    public async Task ExportAsync(Guid userId, string userPublicId, Stream destination, CancellationToken ct)
    {
        var activityRows = await activities.ListAllForExportAsync(userId, ct);
        var equipmentItems = await equipment.ListAsync(userId, ct);

        using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

        await WriteActivitiesCsvAsync(zip, activityRows, ct);
        await WriteEquipmentCsvAsync(zip, equipmentItems, ct);
        await WriteTracksAsync(zip, activityRows, userPublicId, ct);
    }

    private static async Task WriteActivitiesCsvAsync(
        ZipArchive zip, IReadOnlyList<ActivityExportRow> rows, CancellationToken ct)
    {
        var entry = zip.CreateEntry("activities.csv", CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteField("id");
        csv.WriteField("type");
        csv.WriteField("name");
        csv.WriteField("started_at");
        csv.WriteField("ended_at");
        csv.WriteField("local_date");
        csv.WriteField("takeoff_location");
        csv.WriteField("landing_location");
        csv.WriteField("max_altitude_m");
        csv.WriteField("altitude_gain_m");
        csv.WriteField("distance_km");
        csv.WriteField("wind_speed_kmh");
        csv.WriteField("wind_direction");
        csv.WriteField("equipment");
        csv.WriteField("comment");
        await csv.NextRecordAsync();

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();

            csv.WriteField(row.Id);
            csv.WriteField(row.Type == ActivityType.Flight ? "flight" : "ground_handling");
            csv.WriteField(row.Name);
            csv.WriteField(row.StartedAt.ToString("O", CultureInfo.InvariantCulture));
            csv.WriteField(row.EndedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(row.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            csv.WriteField(row.TakeoffLocation ?? "");
            csv.WriteField(row.LandingLocation ?? "");
            csv.WriteField(row.MaxAltitudeM?.ToString(CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(row.AltitudeGainM?.ToString(CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(row.DistanceKm?.ToString(CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(row.WindSpeedKmh?.ToString(CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(row.WindDirection?.ToString(CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(row.EquipmentNames);
            csv.WriteField(row.Comment ?? "");
            await csv.NextRecordAsync();
        }
    }

    private static async Task WriteEquipmentCsvAsync(
        ZipArchive zip, IReadOnlyList<Equipment.Equipment> items, CancellationToken ct)
    {
        var entry = zip.CreateEntry("equipment.csv", CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteField("id");
        csv.WriteField("display_name");
        csv.WriteField("type");
        csv.WriteField("brand");
        csv.WriteField("model");
        csv.WriteField("purchase_date");
        csv.WriteField("next_revision_date");
        csv.WriteField("auto_add");
        csv.WriteField("retired");
        await csv.NextRecordAsync();

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            csv.WriteField(item.Id);
            csv.WriteField(item.DisplayName);
            csv.WriteField(item.Type.ToString());
            csv.WriteField(item.Brand ?? "");
            csv.WriteField(item.Model ?? "");
            csv.WriteField(item.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(item.NextRevisionDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(item.AutoAdd);
            csv.WriteField(item.Retired);
            await csv.NextRecordAsync();
        }
    }

    private async Task WriteTracksAsync(
        ZipArchive zip, IReadOnlyList<ActivityExportRow> rows, string userPublicId, CancellationToken ct)
    {
        foreach (var row in rows)
        {
            if (row.Track is null)
                continue;

            await using var source = await blobStore.OpenTrackAsync(userPublicId, row.Id, row.Track.Format, ct);
            if (source is null)
                continue; // dangling reference shouldn't happen, but export is best-effort, not another integrity check

            var extension = row.Track.Format == TrackFormat.Gpx ? "gpx" : "igc";
            var entry = zip.CreateEntry($"tracks/{row.Id}.{extension}", CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await source.CopyToAsync(entryStream, ct);
        }
    }
}

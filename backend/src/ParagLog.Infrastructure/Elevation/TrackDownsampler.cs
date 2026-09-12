using System.Xml.Linq;
using ParagLog.Core.Activities;

namespace ParagLog.Infrastructure.Elevation;

/// <summary>
/// Extracts a coordinate-only, evenly-downsampled subset of a track for elevation lookups.
/// This is deliberately not the rich client-side parser: no altitude, no time, no stats - just
/// enough to batch-query ground elevation. The backend otherwise never parses tracks.
/// </summary>
public static class TrackDownsampler
{
    // Below the spec's original 300-500: the viewer now linearly interpolates ground elevation
    // between samples (alignElevation.ts) instead of snapping to the nearest one, so a sparser
    // set still renders as a smooth line - and it halves the OpenTopoData batches per upload
    // (100/batch), which is both faster and less likely to hit the public instance's rate limit.
    private const int TargetPointCount = 150;

    public static IReadOnlyList<(double Lat, double Lon)> Downsample(string content, TrackFormat format)
    {
        var points = format == TrackFormat.Gpx ? ExtractGpxPoints(content) : ExtractIgcPoints(content);
        return Downsample(points, TargetPointCount);
    }

    private static List<(double Lat, double Lon)> ExtractGpxPoints(string content)
    {
        var points = new List<(double, double)>();

        XDocument doc;
        try
        {
            doc = XDocument.Parse(content);
        }
        catch
        {
            return points;
        }

        var nodes = doc.Descendants().Where(e => e.Name.LocalName is "trkpt").ToList();
        if (nodes.Count == 0)
            nodes = doc.Descendants().Where(e => e.Name.LocalName is "rtept").ToList();

        foreach (var node in nodes)
        {
            var latText = node.Attribute("lat")?.Value;
            var lonText = node.Attribute("lon")?.Value;
            if (double.TryParse(latText, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(lonText, System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                points.Add((lat, lon));
            }
        }

        return points;
    }

    private static List<(double Lat, double Lon)> ExtractIgcPoints(string content)
    {
        var points = new List<(double, double)>();

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (!line.StartsWith('B') || line.Length < 35)
                continue;

            var lat = ParseLatitude(line.AsSpan(7, 7), line[14]);
            var lon = ParseLongitude(line.AsSpan(15, 8), line[23]);
            if (lat is not null && lon is not null)
                points.Add((lat.Value, lon.Value));
        }

        return points;
    }

    private static double? ParseLatitude(ReadOnlySpan<char> raw, char hemisphere)
    {
        if (!int.TryParse(raw[..2], out var degrees) || !int.TryParse(raw[2..], out var thousandthsOfMinutes))
            return null;

        var value = degrees + thousandthsOfMinutes / 1000.0 / 60.0;
        return hemisphere == 'S' ? -value : value;
    }

    private static double? ParseLongitude(ReadOnlySpan<char> raw, char hemisphere)
    {
        if (!int.TryParse(raw[..3], out var degrees) || !int.TryParse(raw[3..], out var thousandthsOfMinutes))
            return null;

        var value = degrees + thousandthsOfMinutes / 1000.0 / 60.0;
        return hemisphere == 'W' ? -value : value;
    }

    private static IReadOnlyList<(double Lat, double Lon)> Downsample(
        IReadOnlyList<(double Lat, double Lon)> points, int targetCount)
    {
        if (points.Count <= targetCount)
            return points;

        var result = new List<(double, double)>(targetCount);
        for (var i = 0; i < targetCount; i++)
        {
            var sourceIndex = (int)((long)i * (points.Count - 1) / (targetCount - 1));
            result.Add(points[sourceIndex]);
        }

        return result;
    }
}

namespace ParagLog.Core.Abstractions;

public interface IElevationService
{
    /// <summary>
    /// Batch ground-elevation lookup. Returns one entry per input point, in the same order;
    /// an entry is null when the service couldn't resolve that point (e.g. over open ocean).
    /// Best-effort: implementations should not throw for network/rate-limit failures.
    /// </summary>
    Task<IReadOnlyList<double?>> GetElevationsAsync(IReadOnlyList<(double Lat, double Lon)> points, CancellationToken ct);
}

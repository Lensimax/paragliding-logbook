using Microsoft.Extensions.Logging.Abstractions;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;
using ParagLog.Infrastructure.Elevation;

namespace ParagLog.UnitTests.Elevation;

public class ElevationResolverTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private const string UserPublicId = "bob-ab12x";

    private const string SampleGpx =
        "<?xml version=\"1.0\"?><gpx version=\"1.1\"><trk><trkseg>" +
        "<trkpt lat=\"45.86\" lon=\"6.29\"><ele>1000</ele></trkpt>" +
        "<trkpt lat=\"45.87\" lon=\"6.30\"><ele>1100</ele></trkpt>" +
        "</trkseg></trk></gpx>";

    [Fact]
    public async Task TryResolveAsync_writes_elevation_json_and_marks_the_activity()
    {
        var blobStore = new FakeBlobStore(SampleGpx);
        var elevationService = new FakeElevationService(1234.5);
        var activities = new FakeActivityRepository();
        var resolver = new ElevationResolver(blobStore, elevationService, activities, NullLogger<ElevationResolver>.Instance);
        var activityId = Guid.NewGuid();

        await resolver.TryResolveAsync(UserId, UserPublicId, activityId, TrackFormat.Gpx, CancellationToken.None);

        Assert.NotNull(blobStore.SavedElevationJson);
        Assert.Contains("1234.5", blobStore.SavedElevationJson);
        Assert.True(activities.HasElevationSet);
    }

    [Fact]
    public async Task TryResolveAsync_is_a_no_op_when_the_track_blob_is_missing()
    {
        var blobStore = new FakeBlobStore(trackContent: null);
        var elevationService = new FakeElevationService(1000);
        var activities = new FakeActivityRepository();
        var resolver = new ElevationResolver(blobStore, elevationService, activities, NullLogger<ElevationResolver>.Instance);

        await resolver.TryResolveAsync(UserId, UserPublicId, Guid.NewGuid(), TrackFormat.Gpx, CancellationToken.None);

        Assert.Null(blobStore.SavedElevationJson);
        Assert.False(activities.HasElevationSet);
    }

    [Fact]
    public async Task TryResolveAsync_swallows_elevation_service_failures()
    {
        var blobStore = new FakeBlobStore(SampleGpx);
        var elevationService = new ThrowingElevationService();
        var activities = new FakeActivityRepository();
        var resolver = new ElevationResolver(blobStore, elevationService, activities, NullLogger<ElevationResolver>.Instance);

        await resolver.TryResolveAsync(UserId, UserPublicId, Guid.NewGuid(), TrackFormat.Gpx, CancellationToken.None);

        Assert.Null(blobStore.SavedElevationJson);
        Assert.False(activities.HasElevationSet);
    }

    private sealed class FakeElevationService(double elevation) : IElevationService
    {
        public Task<IReadOnlyList<double?>> GetElevationsAsync(
            IReadOnlyList<(double Lat, double Lon)> points, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<double?>>(points.Select(_ => (double?)elevation).ToList());
    }

    private sealed class ThrowingElevationService : IElevationService
    {
        public Task<IReadOnlyList<double?>> GetElevationsAsync(
            IReadOnlyList<(double Lat, double Lon)> points, CancellationToken ct) =>
            throw new HttpRequestException("simulated network failure");
    }

    private sealed class FakeBlobStore(string? trackContent) : IBlobStore
    {
        public string? SavedElevationJson { get; private set; }

        public Task SaveTrackAsync(string userPublicId, Guid activityId, TrackFormat format, Stream content, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<Stream?> OpenTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct) =>
            Task.FromResult(trackContent is null
                ? null
                : (Stream?)new MemoryStream(System.Text.Encoding.UTF8.GetBytes(trackContent)));

        public Task DeleteTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct) =>
            throw new NotSupportedException();

        public async Task SaveElevationAsync(string userPublicId, Guid activityId, Stream content, CancellationToken ct)
        {
            using var reader = new StreamReader(content);
            SavedElevationJson = await reader.ReadToEndAsync(ct);
        }

        public Task<Stream?> OpenElevationAsync(string userPublicId, Guid activityId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task DeleteActivityFolderAsync(string userPublicId, Guid activityId, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        public bool HasElevationSet { get; private set; }

        public Task<Activity?> FindByIdAsync(Guid userId, Guid activityId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<ActivityPage> ListAsync(Guid userId, ActivityListQuery query, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<Activity> CreateAsync(Guid userId, CreateActivityCommand command, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<Activity?> UpdateAsync(Guid userId, Guid activityId, UpdateActivityCommand command, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid userId, Guid activityId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<Activity?> SetTrackAsync(Guid userId, Guid activityId, TrackReference? track, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<Activity?> SetHasElevationAsync(Guid userId, Guid activityId, bool hasElevation, CancellationToken ct)
        {
            HasElevationSet = hasElevation;
            return Task.FromResult<Activity?>(null);
        }
    }
}
